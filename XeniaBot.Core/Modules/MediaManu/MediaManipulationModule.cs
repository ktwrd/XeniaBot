using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using NetVips;
using XeniaBot.Core.Helpers;

using NVImage = NetVips.Image;
using Log = XeniaBot.Shared.Log;

namespace XeniaBot.Core.Modules;

public partial class MediaManipulationModule : InteractionModuleBase
{
    [SlashCommand("caption", "Add a caption to a piece of media")]
    public async Task Caption(string caption,
        IAttachment attachment,
        [Discord.Interactions.Summary(description: "Export as a GIF")]
        bool saveAsGif = false)
    {
        await Context.Interaction.DeferAsync();
        
        var embed = new EmbedBuilder()
            .WithTitle("Media Caption")
            .WithCurrentTimestamp();
        byte[] dataBytes = Array.Empty<byte>();
        string fileType = "";
        try
        {
            (dataBytes, fileType) = await MediaManipulationHelper.GetUrlBytes(attachment);
            if (fileType.Length < 1)
                throw new Exception("Invalid attachment type");
            if (dataBytes.Length < 1)
                throw new Exception("Empty data");
        }
        catch (Exception ex)
        {
            embed.WithDescription($"Failed to get attachment\n```\n{ex.Message}\n```")
                 .WithColor(Color.Red);
            await FollowupAsync(embed: embed.Build());
            return;
        }

        try
        {
            var originalData = new MemoryStream(dataBytes);

            await AttemptFontExtract();

            var opts = new VOption();
            opts.Add("access", 1);
            bool isAnimated = MediaManipulationHelper.IsAnimatedType(fileType);
            if (isAnimated)
            {
                opts.Add("n", -1);
            }

            var imageToCaption = NetVipsHelper.Normalize(NVImage.NewFromStream(originalData, kwargs: opts));
            
            var width = imageToCaption.Width;
            var pageHeight = imageToCaption.PageHeight;
            var nPages = NetVipsHelper.GetNPages(imageToCaption);
                
            var fontSize = imageToCaption.Width / 10f;
            var textWidth = imageToCaption.Width - ((imageToCaption.Width / 25) * 2);

            // Create caption text
            var text = NVImage.Text(
                text: $"<span background=\"white\">{caption}</span>",
                rgba: true,
                align: Enums.Align.Centre,
                font: $"FuturaExtraBlackCondensed {fontSize}px",
                fontfile: GetFontLocation("font_caption"),
                width: textWidth);
            
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
            
            // Append vertically `imageToCaption` to `text`, with gif support.
            var imgList = new NVImage[nPages];
            for (int i = 0; i < nPages; i++)
            {
                var imgFrame = isAnimated
                    ? imageToCaption.Crop(0, i * pageHeight, width, pageHeight)
                    : imageToCaption;

                var frame = captionImage.Join(imgFrame, Enums.Direction.Vertical, background: new double[] {255, 255, 255, 0}, expand: true);
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
            Log.Debug("Complete");
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
            Log.Error(ex);
            embed.WithDescription($"Failed to run task.\n```\n{ex.Message}\n```").WithColor(Color.Red);
            await FollowupAsync(embed: embed.Build());
            await DiscordHelper.ReportError(ex, Context);
            return;
        }
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
        } else if (resize)
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
                var padded = imgFrame.Embed(0, 0, width, pageHeight + 15, background: WhiteRGBA);
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
    public async Task SpeechBubble(IAttachment attachment,
        [Discord.Interactions.Summary(description: "When True, the speech bubble will be on the bottom, and when False it will be on top.")]
        bool flip = false,
        [Discord.Interactions.Summary(description: "When True, the speech bubble will have a transparent background, or it will have a white background")]
        bool alpha = false,
        [Discord.Interactions.Summary(description: "Export as a GIF")]
        bool saveAsGif = false)
    {
        await Context.Interaction.DeferAsync();
        
        var embed = new EmbedBuilder()
            .WithTitle("Speech Bubble")
            .WithCurrentTimestamp();
        byte[] dataBytes = Array.Empty<byte>();
        string fileType = "";
        try
        {
            (dataBytes, fileType) = await MediaManipulationHelper.GetUrlBytes(attachment);
            if (fileType.Length < 1)
                throw new Exception("Invalid attachment type");
            if (dataBytes.Length < 1)
                throw new Exception("Empty data");
        }
        catch (Exception ex)
        {
            embed.WithDescription($"Failed to get attachment\n```\n{ex.Message}\n```")
                 .WithColor(Color.Red);
            await FollowupAsync(embed: embed.Build());
            return;
        }

        try
        {
            var originalData = new MemoryStream(dataBytes);

            await AttemptFontExtract();

            var opts = new VOption();
            opts.Add("access", 1);
            bool isAnimated = MediaManipulationHelper.IsAnimatedType(fileType);
            if (isAnimated)
            {
                opts.Add("n", -1);
            }

            var sourceImage = NetVipsHelper.Normalize(NVImage.NewFromStream(originalData, kwargs: opts));
            var watermark = NetVipsHelper.Normalize(alpha
                ? NVImage.NewFromStream(MediaResources.ImageSpeech)
                : NVImage.NewFromStream(MediaResources.ImageSpeechBubble));
            var final = await Watermark(
                sourceImage, watermark, 2, isAnimated, resize: true, yscale: 0.2f, alpha: alpha, flip: flip);
            
            
            Log.Debug($"Complete");
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
            Log.Error(ex);
            embed.WithDescription($"Failed to run task.\n```\n{ex.Message}\n```").WithColor(Color.Red);
            await FollowupAsync(embed: embed.Build());
            await DiscordHelper.ReportError(ex, Context);
            return;
        }
    }
}