using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageEmbedThumbnail
{
    public string Url { get; set; }
    public string ProxyUrl { get; set; }
    public int? Height { get; set; }
    public int? Width { get; set; }

    public CacheMessageEmbedThumbnail()
    {
        Url = "";
        ProxyUrl = "";
    }

    public CacheMessageEmbedThumbnail Update(EmbedThumbnail? thumbnail)
    {
        var t = thumbnail.GetValueOrDefault();
        if (t == null)
            return this;

        this.Url = t.Url;
        this.ProxyUrl = t.ProxyUrl;
        this.Height = t.Height;
        this.Width = t.Width;
        return this;
    }
    public static CacheMessageEmbedThumbnail? FromExisting(EmbedThumbnail? thumbnail)
    {
        if (thumbnail == null)
            return null;

        var instance = new CacheMessageEmbedThumbnail();
        return instance.Update(thumbnail);
    }
}