using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageActivity
{
    public new MessageActivityType Type { get; set; }
    public new string PartyId { get; set; }

    public CacheMessageActivity()
    {
        PartyId = "";
    }
}