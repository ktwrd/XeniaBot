using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageComponent : IMessageComponent
{
    public ComponentType Type { get; set; }
    public string CustomId { get; set; }

    public CacheMessageComponent()
    {
        CustomId = "";
    }
}