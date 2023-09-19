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
}