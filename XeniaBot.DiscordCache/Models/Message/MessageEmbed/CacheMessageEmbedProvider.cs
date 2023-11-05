using Discord;

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

    public CacheMessageEmbedProvider Update(EmbedProvider? provider)
    {
        var p = provider.GetValueOrDefault();
        if (p == null)
            return this;

        this.Name = p.Name;
        this.Url = p.Url;
        return this;
    }
    public static CacheMessageEmbedProvider FromExisting(EmbedProvider? provider)
    {
        if (provider == null)
            return null;

        var instance = new CacheMessageEmbedProvider();
        return instance.Update(provider);
    }
}