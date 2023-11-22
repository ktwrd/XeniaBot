using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheEmote
{
    public string? Name { get; set; }

    public CacheEmote()
    {
        Name = null;
    }
    public static CacheEmote? FromExisting(IEmote? emote)
    {
        if (emote == null)
            return null;

        var instance = new CacheEmote();
        return instance.Update(emote);
    }
    public CacheEmote Update(IEmote emote)
    {
        Name = emote.Name;
        return this;
    }
}