namespace XeniaBot.DiscordCache.Models;

public class CacheMessageEmbedProvider
{
    public string Name { get; set; }
    public string Url { get; set; }

    public CacheMessageEmbedProvider()
    {
        Name = "";
        Url = "";
    }
}