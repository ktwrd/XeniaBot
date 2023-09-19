using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheGuildEmote : CacheEmote
{
    public bool IsManaged { get; set; }
    public bool RequireColons { get; set; }
    public ulong[] RoleIds { get; set; }
    public ulong? CreatorId { get; set; }

    public CacheGuildEmote FromExisting(GuildEmote emote)
    {
        base.FromExisting(emote);
        IsManaged = emote.IsManaged;
        RequireColons = emote.RequireColons;
        RoleIds = emote.RoleIds.ToArray();
        CreatorId = emote.CreatorId;
        return this;
    }
}