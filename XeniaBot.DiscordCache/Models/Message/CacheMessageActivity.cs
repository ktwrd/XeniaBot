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

    public CacheMessageActivity Update(MessageActivity activity)
    {
        Type = activity.Type;
        PartyId = activity.PartyId;
        return this;
    }

    public static CacheMessageActivity? FromExisting(MessageActivity? activity)
    {
        if (activity == null)
            return null;

        var instance = new CacheMessageActivity();
        return instance.Update(activity);
    }
}