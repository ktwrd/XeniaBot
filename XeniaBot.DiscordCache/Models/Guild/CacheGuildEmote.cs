using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheGuildEmote : CacheEmote
{
    public bool IsManaged { get; set; }
    public bool RequireColons { get; set; }
    public ulong[] RoleIds { get; set; }
    public ulong? CreatorId { get; set; }

    public CacheGuildEmote()
        : base()
    {
        RoleIds = Array.Empty<ulong>();
    }
    public CacheGuildEmote Update(GuildEmote emote)
    {
        base.Update(emote);
        IsManaged = emote.IsManaged;
        RequireColons = emote.RequireColons;
        RoleIds = emote.RoleIds.ToArray();
        CreatorId = emote.CreatorId;
        return this;
    }
    public static CacheGuildEmote? FromExisting(GuildEmote? emote)
    {
        if (emote == null)
            return null;

        var instance = new CacheGuildEmote();
        return instance.Update(emote);
    }
}