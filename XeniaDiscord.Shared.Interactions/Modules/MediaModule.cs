using Discord;
using Discord.Interactions;
using NetVips;
using NLog;
using XeniaBot.Shared.Helpers;
using XeniaDiscord.Shared.Interactions.Helpers;
using NVImage = NetVips.Image;

namespace XeniaDiscord.Shared.Interactions.Modules;

public class MediaModule : InteractionModuleBase
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    [SlashCommand("caption", "Add a caption to a piece of media")]
    public async Task CaptionCommand(string caption,
        IAttachment attachment,
        [Summary(description: "Export as a GIF")]
        bool saveAsGif = false)
    {
        var disposeObjects = new List<IDisposable?>();
        void DisposeObjects()
        {
            disposeObjects.Reverse();
            foreach (var item in disposeObjects)
            {
                try
                {
                    item?.Dispose();
                }
                catch { }
            }
        }
        await DeferAsync();

        MemoryStream attachmentStream;
        try
        {
            attachmentStream = await MediaModuleHelper.FetchData(attachment);
            attachmentStream.Seek(0, SeekOrigin.Begin);
            disposeObjects.Add(attachmentStream);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to fetch attachment {attachment.Url} (id: {attachment.Id}, content-type: {attachment.ContentType})");
            var embed = new EmbedBuilder()
                .WithTitle("Failed to download attachment")
                .WithDescription(ex.Message.Substring(0, Math.Min(ex.Message.Length, 1900)))
                .WithColor(Color.Red);
            if (SentrySdk.IsEnabled)
            {
                var id = SentrySdk.CaptureException(ex, scope =>
                {
                    scope.SetInteractionInfo(Context);
                    scope.SetExtra("param.attachment.id", attachment.Id);
                    scope.SetExtra("param.attachment.url", attachment.Url);
                    scope.SetExtra("param.attachment.proxy_url", attachment.ProxyUrl);
                });
                embed.WithFooter(id.ToString());
            }
            await FollowupAsync(embed: embed.Build());
            DisposeObjects();
            return;
        }
        try
        {
            await MediaModuleHelper.AttemptFontExtract();
            var isAnimated = MediaModuleHelper.IsAnimatedType(attachment.ContentType);

            var opts = new VOption();
            opts.Add("access", 1);
            if (isAnimated)
            {
                opts.Add("n", -1);
            }

            var imageToCaption = NetVipsHelper.Normalize(NVImage.NewFromStream(attachmentStream, kwargs: opts));
            disposeObjects.Add(imageToCaption);

            var width = imageToCaption.Width;
            var pageHeight = imageToCaption.PageHeight;
            var nPages = NetVipsHelper.GetNPages(imageToCaption);

            var fontSize = imageToCaption.Width / 10f;
            var textWidth = imageToCaption.Width - ((imageToCaption.Width / 25) * 2);

            // Create caption text
            var fontLocation = MediaModuleHelper.GetFontLocation("font_caption");
            _log.Trace("Using font: " + fontLocation);
            var text = NVImage.Text(
                text: $"<span background=\"white\">{caption}</span>",
                rgba: true,
                align: Enums.Align.Centre,
                font: $"FuturaExtraBlackCondensed {fontSize}px",
                fontfile: fontLocation,
                width: textWidth);
            disposeObjects.Add(text);

            var zeroVec = new double[]
            { 0, 0, 0, 0 };

            // Align text and make sure all transparent stuff is white
            var captionImage = ((text.Equal(zeroVec)).BandAnd())
                .Ifthenelse(255, text)
                .Gravity(
                    Enums.CompassDirection.Centre,
                    imageToCaption.Width,
                    text.Height + 24,
                    extend: Enums.Extend.White);
            disposeObjects.Add(captionImage);

            // Append vertically `imageToCaption` to `text`, with gif support.
            var imgList = new NVImage[nPages];
            for (int i = 0; i < nPages; i++)
            {
                var imgFrame = isAnimated
                    ? imageToCaption.Crop(0, i * pageHeight, width, pageHeight)
                    : imageToCaption;
                disposeObjects.Add(imgFrame);

                var frame = captionImage.Join(imgFrame, Enums.Direction.Vertical, background: new double[] { 255, 255, 255, 0 }, expand: true);
                disposeObjects.Add(frame);
                imgList[i] = frame;
            }

            // Make sure that the image height is the actual
            // height instead of the whole animation height.
            var textHeight = captionImage.Height;
            var final = NVImage.Arrayjoin(imgList, across: 1)
                .Mutate((i) =>
                {
                    if (nPages > 1)
                        i.Set("page-height", pageHeight + textHeight);
                });
            disposeObjects.Add(final);
            if (isAnimated || saveAsGif)
            {
                using var gifStream = new MemoryStream(final.GifsaveBuffer(dither: 1, bitdepth: 8, interlace: true));
                await FollowupWithFileAsync(gifStream, $"{Context.Interaction.Id}.gif");
            }
            else
            {
                using var pngStream = new MemoryStream(final.PngsaveBuffer(compression: 4, dither: 1, bitdepth: 8, interlace: true));
                await FollowupWithFileAsync(pngStream, $"{Context.Interaction.Id}.png");
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to process image {attachment.Url} ({attachment.Id}) " +
                           $"in channel {Context.Interaction.ChannelId} " +
                           $"in guild {Context.Interaction.GuildId} " +
                           $"from user \"{Context.Interaction.User.GlobalName}\" ({Context.Interaction.User.Username}, {Context.Interaction.User.Id})");

            var embed = new EmbedBuilder()
                .WithTitle("Caption - Failed to process image")
                .WithDescription(ex.Message[..Math.Min(ex.Message.Length, 1900)])
                .WithColor(Color.Red);
            if (SentrySdk.IsEnabled)
            {
                var id = SentrySdk.CaptureException(ex, scope =>
                {
                    scope.SetInteractionInfo(Context);
                    scope.SetExtra("param.attachment.id", attachment.Id);
                    scope.SetExtra("param.attachment.url", attachment.Url);
                    scope.SetExtra("param.attachment.proxy_url", attachment.ProxyUrl);
                });
                embed.WithFooter(id.ToString());
            }
            await FollowupAsync(embed: embed.Build());
        }
        DisposeObjects();
    }


    public async Task<NVImage> Watermark(NVImage source,
        NVImage watermark,
        int gravity,
        bool isGif,
        bool resize = false,
        float yscale = 0f,
        bool append = false,
        bool alpha = false,
        bool flip = false,
        bool mc = false)
    {
        int width = source.Width;
        int pageHeight = source.PageHeight;
        int nPages = NetVipsHelper.GetNPages(source);

        if (flip)
        {
            watermark = watermark.Flip(Enums.Direction.Horizontal);
        }

        if (resize && append)
        {
            watermark = watermark.Resize((double)width / (double)watermark.Width);
        }
        else if (resize && yscale > 0)
        {
            watermark = watermark.Resize((double)width / (double)watermark.Width, vscale: (double)(pageHeight * yscale) / (double)watermark.Height);
        }
        else if (resize)
        {
            watermark = watermark.Resize((double)pageHeight / (double)watermark.Height);
        }

        int x = 0;
        int y = 0;
        switch (gravity)
        {
            case 1:
                break;
            case 2:
                x = (width / 2) - (watermark.Width / 2);
                break;
            case 3:
                x = width - watermark.Width;
                break;
            case 5:
                x = (width / 2) - (watermark.Width / 2);
                y = (pageHeight / 2) - (watermark.Height / 2);
                break;
            case 6:
                x = width - watermark.Width;
                y = (pageHeight / 2) - (watermark.Height / 2);
                break;
            case 8:
                x = (width / 2) - (watermark.Width / 2);
                y = pageHeight - watermark.Height;
                break;
            case 9:
                x = width - watermark.Width;
                y = pageHeight - watermark.Height;
                break;
        }

        var img = new NVImage[nPages];
        int addedHeight = 0;
        NVImage? contentAlpha = null;
        NVImage? frameAlpha = null;
        NVImage? bg = null;
        NVImage? frame = null;
        for (int i = 0; i < nPages; i++)
        {
            var imgFrame = isGif ? source.Crop(0, i * pageHeight, width, pageHeight) : source;
            if (append)
            {
                var appended = imgFrame.Join(watermark, direction: Enums.Direction.Vertical, expand: true);
                addedHeight = watermark.Height;
                img[i] = imgFrame;
            }
            else if (mc)
            {
                var padded = imgFrame.Embed(0, 0, width, pageHeight + 15, background: MediaModuleHelper.WhiteRGBA);
                var composited = padded.Composite2(
                    watermark, Enums.BlendMode.Over, x: width - 190, y: padded.Height - 22);
                addedHeight = 15;
                img[i] = composited;
            }
            else
            {
                NVImage? composited = null;
                if (alpha)
                {
                    if (i == 0)
                    {
                        contentAlpha = watermark.ExtractBand(0).Embed(
                            x, y, width, pageHeight, extend: Enums.Extend.White);
                        frameAlpha = watermark.ExtractBand(1).Embed(
                            x, y, width, pageHeight, extend: Enums.Extend.Black);
                        bg = frameAlpha.NewFromImage(
                            new double[]
                            {
                                0, 0, 0
                            }).Copy(interpretation: Enums.Interpretation.Srgb);
                        frame = bg.Bandjoin(frameAlpha);
                    }

                    var content = imgFrame.ExtractBand(0, n: 3).Bandjoin(contentAlpha & imgFrame.ExtractBand(3));
                    composited = content.Composite2(frame, Enums.BlendMode.Over, x: x, y: y);
                }
                else
                {
                    composited = imgFrame.Composite2(watermark, Enums.BlendMode.Over, x: x, y: y);
                }

                img[i] = composited;
            }
        }


        var final = NVImage.Arrayjoin(img, across: 1)
            .Mutate((i) =>
            {
                if (nPages > 1)
                    i.Set("page-height", pageHeight + addedHeight);
            });
        return final;
    }

    [SlashCommand("speechbubble", "Add a speech bubble to an image or a gif.")]
    public async Task SpeechBubbleCommand(
        IAttachment attachment,
        [Summary(description: "When True, the speech bubble will be on the bottom, and when False it will be on top.")]
        bool flip = false,
        [Summary(description: "When True, the speech bubble will have a transparent background, or it will have a white background")]
        bool alpha = false,
        [Summary(description: "Export as a GIF")]
        bool saveAsGif = false)
    {
        var disposeObjects = new List<IDisposable?>();
        void DisposeObjects()
        {
            disposeObjects.Reverse();
            foreach (var item in disposeObjects)
            {
                try
                {
                    item?.Dispose();
                }
                catch { }
            }
        }
        await DeferAsync();

        MemoryStream attachmentStream;
        try
        {
            attachmentStream = await MediaModuleHelper.FetchData(attachment);
            attachmentStream.Seek(0, SeekOrigin.Begin);
            disposeObjects.Add(attachmentStream);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to fetch attachment {attachment.Url} (id: {attachment.Id}, content-type: {attachment.ContentType})");
            var embed = new EmbedBuilder()
                .WithTitle("Failed to download attachment")
                .WithDescription(ex.Message[..Math.Min(ex.Message.Length, 1900)])
                .WithColor(Color.Red);
            if (SentrySdk.IsEnabled)
            {
                var id = SentrySdk.CaptureException(ex, scope =>
                {
                    scope.SetInteractionInfo(Context);
                    scope.SetExtra("param.attachment.id", attachment.Id);
                    scope.SetExtra("param.attachment.url", attachment.Url);
                    scope.SetExtra("param.attachment.proxy_url", attachment.ProxyUrl);
                });
                embed.WithFooter(id.ToString());
            }
            await FollowupAsync(embed: embed.Build());
            DisposeObjects();
            return;
        }

        try
        {
            await MediaModuleHelper.AttemptFontExtract();
            var opts = new VOption();
            opts.Add("access", 1);
            bool isAnimated = MediaModuleHelper.IsAnimatedType(attachment.ContentType);
            if (isAnimated)
            {
                opts.Add("n", -1);
            }

            var sourceImage = NetVipsHelper.Normalize(NVImage.NewFromStream(attachmentStream, kwargs: opts));
            disposeObjects.Add(sourceImage);
            var watermark = NetVipsHelper.Normalize(alpha
                ? NVImage.NewFromStream(MediaModuleResources.ImageSpeech)
                : NVImage.NewFromStream(MediaModuleResources.ImageSpeechBubble));
            disposeObjects.Add(watermark);
            var final = await Watermark(
                sourceImage, watermark, 2, isAnimated, resize: true, yscale: 0.2f, alpha: alpha, flip: flip);
            disposeObjects.Add(final);

            if (isAnimated || saveAsGif)
            {
                using var gifStream = new MemoryStream(final.GifsaveBuffer(dither: 1, bitdepth: 8, interlace: true));
                await FollowupWithFileAsync(gifStream, $"{Context.Interaction.Id}.gif");
            }
            else
            {
                using var pngStream = new MemoryStream(final.PngsaveBuffer(compression: 4, dither: 1, bitdepth: 8, interlace: true));
                await FollowupWithFileAsync(pngStream, $"{Context.Interaction.Id}.png");
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to process image {attachment.Url} ({attachment.Id}) " +
                           $"in channel {Context.Interaction.ChannelId} " +
                           $"in guild {Context.Interaction.GuildId} " +
                           $"from user \"{Context.Interaction.User.GlobalName}\" ({Context.Interaction.User.Username}, {Context.Interaction.User.Id})");
            var embed = new EmbedBuilder()
                .WithTitle("Failed to process image")
                .WithDescription(ex.Message[..Math.Min(ex.Message.Length, 1900)])
                .WithColor(Color.Red);
            if (SentrySdk.IsEnabled)
            {
                var id = SentrySdk.CaptureException(ex, scope =>
                {
                    scope.SetInteractionInfo(Context);
                    scope.SetExtra("param.attachment.id", attachment.Id);
                    scope.SetExtra("param.attachment.url", attachment.Url);
                    scope.SetExtra("param.attachment.proxy_url", attachment.ProxyUrl);
                });
                embed.WithFooter(id.ToString());
            }
            await FollowupAsync(embed: embed.Build());
        }
        DisposeObjects();
    }

    [SlashCommand("1984", "1984 calendar")]
    public async Task Command1984(
        string caption,
        string? date = null,
        [Summary(description: "Export as a GIF")]
        bool saveAsGif = false)
    {
        await DeferAsync();

        var disposeObjects = new List<IDisposable?>();
        void DisposeObjects()
        {
            disposeObjects.Reverse();
            foreach (var item in disposeObjects)
            {
                try
                {
                    item?.Dispose();
                }
                catch { }
            }
        }

        try
        {
            var originalDate = string.IsNullOrEmpty(date) || date.Equals("january 1984", StringComparison.InvariantCultureIgnoreCase);

            var img = originalDate
                ? NVImage.NewFromStream(MediaModuleResources.Image1984OriginalDate)
                : NVImage.NewFromStream(MediaModuleResources.Image1984);
            disposeObjects.Add(img);

            var speechFontLocation = MediaModuleHelper.GetFontLocation("font_AtkinsonHyperlegible_Bold");
            var speechBubble = NVImage.Text(
                caption,
                font: "Atkinson Hyperlegible Bold",
                rgba: true,
                fontfile: speechFontLocation,
                align: Enums.Align.Centre,
                width: 290,
                height: 90);
            speechBubble.Gravity(Enums.CompassDirection.Centre, 290, 90, extend: Enums.Extend.Black);
            disposeObjects.Add(speechBubble);
            img = img.Composite2(speechBubble, Enums.BlendMode.Over, x: 60, y: 20);
            disposeObjects.Add(img);

            if (!originalDate)
            {
                var dateFontLocation = MediaModuleHelper.GetFontLocation("font_ImpactMix");
                var dateText = NVImage.Text(
                    $"<span color='black'>{date!}</span>",
                    font: "ImpactMix",
                    rgba: true,
                    fontfile: dateFontLocation,
                    align: Enums.Align.Centre,
                    width: 124,
                    height: 34);
                disposeObjects.Add(dateText);
                dateText = dateText.Gravity(Enums.CompassDirection.Centre, 124, 34, extend: Enums.Extend.Black);
                disposeObjects.Add(dateText);
                dateText = dateText.Affine(new double[] { 1, 0, 0.176327, 1 });
                disposeObjects.Add(dateText);
                img = img.Composite2(dateText, Enums.BlendMode.Over, x: 454, y: 138);
                disposeObjects.Add(img);
                img = img.Composite2(NetVipsHelper.Normalize(NVImage.NewFromStream(MediaModuleResources.Image1984Cover)), Enums.BlendMode.Over);
                disposeObjects.Add(img);
            }
            if (saveAsGif)
            {
                using var gifStream = new MemoryStream(img.GifsaveBuffer(dither: 1, bitdepth: 8, interlace: true));
                await FollowupWithFileAsync(gifStream, $"{Context.Interaction.Id}.gif");
            }
            else
            {
                using var pngStream = new MemoryStream(img.PngsaveBuffer(compression: 4, dither: 1, bitdepth: 8, interlace: true));
                await FollowupWithFileAsync(pngStream, $"{Context.Interaction.Id}.png");
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to process image with caption \"{caption}\" and date \"{date}\" " +
                           $"in channel {Context.Interaction.ChannelId} " +
                           $"in guild {Context.Interaction.GuildId} " +
                           $"from user \"{Context.Interaction.User.GlobalName}\" ({Context.Interaction.User.Username}, {Context.Interaction.User.Id})");
            var embed = new EmbedBuilder()
                .WithTitle("Failed to create image")
                .WithDescription(ex.Message[..Math.Min(ex.Message.Length, 1900)])
                .WithColor(Color.Red);
            if (SentrySdk.IsEnabled)
            {
                var id = SentrySdk.CaptureException(ex, scope =>
                {
                    scope.SetInteractionInfo(Context);
                    scope.SetExtra("param.caption", caption);
                    scope.SetExtra("param.date", date);
                    scope.SetExtra("param.save_as_gif", saveAsGif);
                });
                embed.WithFooter(id.ToString());
            }
            await FollowupAsync(embed: embed.Build());
        }
        DisposeObjects();
    }
}
