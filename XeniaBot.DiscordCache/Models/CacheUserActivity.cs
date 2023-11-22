using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheUserActivity : IActivity
{
    public string Name { get; set; }
    public ActivityType Type { get; set; }
    public ActivityProperties Flags { get; set; }
    public string? Details { get; set; }

    public CacheUserActivity Update(IActivity activity)
    {
        Name = activity.Name;
        Type = activity.Type;
        Flags = activity.Flags;
        Details = activity.Details;
        return this;
    }

    public static CacheUserActivity? FromExisting(IActivity? activity)
    {
        if (activity == null)
            return null;

        var instance = new CacheUserActivity();
        return instance.Update(activity);
    }
}