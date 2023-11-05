using Discord;

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

    public CacheMessageEmbedFooter Update(EmbedFooter? footer)
    {
        var f = footer.GetValueOrDefault();
        if (f == null)
            return this;

        this.Text = f.Text;
        this.IconUrl = f.IconUrl;
        this.ProxyUrl = f.ProxyUrl;
        return this;
    }
    public static CacheMessageEmbedFooter? FromExisting(EmbedFooter? footer)
    {
        if (footer == null)
            return null;

        var instance = new CacheMessageEmbedFooter();
        return instance.Update(footer);
    }
}