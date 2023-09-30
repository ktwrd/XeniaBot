using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheEmote
{
    public string Name { get; set; }

    public CacheEmote FromExisting(IEmote emote)
    {
        Name = emote.Name;
        return this;
    }
}