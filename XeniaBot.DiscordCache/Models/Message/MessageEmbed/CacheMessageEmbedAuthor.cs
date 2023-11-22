using Discord;

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

    public CacheMessageEmbedAuthor Update(EmbedAuthor? author)
    {
        var a = author.GetValueOrDefault();
        if (a == null)
            return this;
        this.Name = a.Name;
        this.Url = a.Url;
        this.IconUrl = a.IconUrl;
        this.ProxyIconUrl = a.ProxyIconUrl;
        return this;
    }
    public static CacheMessageEmbedAuthor? FromExisting(EmbedAuthor? author)
    {
        if (author == null)
            return null;
        var instance = new CacheMessageEmbedAuthor();
        return instance.Update(author);
    }
}