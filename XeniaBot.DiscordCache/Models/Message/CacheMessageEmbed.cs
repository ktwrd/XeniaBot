using System.Text.Json;
using Discord;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageEmbed
{
    public string Url { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public EmbedType Type { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public string? Color { get; set; }
    public EmbedImage? Image { get; set; }
    public EmbedVideo? Video { get; set; }
    public CacheMessageEmbedAuthor? Author { get; set; }
    public CacheMessageEmbedFooter? Footer { get; set; }
    public CacheMessageEmbedProvider? Provider { get; set; }
    public CacheMessageEmbedThumbnail? Thumbnail { get; set; }
    public CacheMessageEmbedField[] Fields { get; set; }

    public CacheMessageEmbed()
    {
        Url = "";
        Title = "";
        Description = "";
        Fields = Array.Empty<CacheMessageEmbedField>();
    }

    public CacheMessageEmbed Update(IEmbed? embed)
    {
        if (embed == null)
            return this;
        
        this.Url = embed.Url;
        this.Title = embed.Title;
        this.Description = embed.Description;
        this.Type = embed.Type;
        this.Timestamp = embed.Timestamp;
        this.Color = embed.Color == null ? null : XeniaHelper.ToHex(embed.Color ?? new Discord.Color(0, 0, 0));
        this.Image = embed.Image;
        this.Video = embed.Video;
        this.Author = CacheMessageEmbedAuthor.FromExisting(embed.Author);
        this.Footer = CacheMessageEmbedFooter.FromExisting(embed.Footer);
        this.Provider = CacheMessageEmbedProvider.FromExisting(embed.Provider);
        this.Thumbnail = CacheMessageEmbedThumbnail.FromExisting(embed.Thumbnail);

        if (embed.Fields == null)
        {
            this.Fields = Array.Empty<CacheMessageEmbedField>();
        }
        else
        {
            var fieldList = new List<CacheMessageEmbedField>();
            foreach (var item in embed.Fields)
            {
                fieldList.Add(CacheMessageEmbedField.FromExisting(item));
            }

            this.Fields = fieldList.ToArray();
        }
        
        return this;
    }
    public static CacheMessageEmbed? FromExisting(IEmbed? embed)
    {
        if (embed == null)
            return null;

        var instance = new CacheMessageEmbed();
        return instance.Update(embed);
    }
}