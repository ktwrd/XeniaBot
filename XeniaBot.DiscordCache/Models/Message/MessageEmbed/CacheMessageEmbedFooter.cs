namespace XeniaBot.DiscordCache.Models;

public class CacheMessageEmbedFooter
{
    public string Text { get; set; }    
    public string IconUrl { get; set; }
    public string ProxyUrl { get; set; }

    public CacheMessageEmbedFooter()
    {
        Text = "";
        IconUrl = "";
        ProxyUrl = "";
    }
}