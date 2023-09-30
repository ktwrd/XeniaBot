namespace XeniaBot.DiscordCache.Models;

public class CacheMessageEmbedAuthor
{
    public string Name { get; set; }
    public string Url { get; set; }
    public string IconUrl { get; set; }
    public string ProxyIconUrl { get; set; }

    public CacheMessageEmbedAuthor()
    {
        Name = "";
        Url = "";
        IconUrl = "";
        ProxyIconUrl = "";
    }
}