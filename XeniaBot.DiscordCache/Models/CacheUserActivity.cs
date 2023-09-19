using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheUserActivity : IActivity
{
    public string Name { get; set; }
    public ActivityType Type { get; set; }
    public ActivityProperties Flags { get; set; }
    public string Details { get; set; }
}