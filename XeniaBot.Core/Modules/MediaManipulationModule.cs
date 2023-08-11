using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using NetVips;
using XeniaBot.Core.Helpers;
using Image = NetVips.Image;
using Log = XeniaBot.Shared.Log;

namespace XeniaBot.Core.Modules;

public partial class MediaManipulationModule : InteractionModuleBase
{
    [SlashCommand("caption", "Add a caption to a piece of media")]
    public async Task Caption(string caption, IAttachment attachment)
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

            var imageToCaption = NetVipsHelper.Normalize(Image.NewFromStream(originalData, kwargs: opts));
            
            var width = imageToCaption.Width;
            var pageHeight = imageToCaption.PageHeight;
            var nPages = NetVipsHelper.GetNPages(imageToCaption);
                
            var fontSize = imageToCaption.Width / 10f;
            var textWidth = imageToCaption.Width - ((imageToCaption.Width / 25) * 2);

            // Create caption text
            var text = Image.Text(
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
            var imgList = new Image[nPages];
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
            var final = Image.Arrayjoin(imgList, across: 1)
                .Mutate((i) =>
                {
                    i.Set("page-height", pageHeight + textHeight);
                });
            Log.Debug($"Complete");
            if (isAnimated)
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