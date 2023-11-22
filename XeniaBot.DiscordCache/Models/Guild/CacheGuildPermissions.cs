using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheGuildPermissions
{
    /// <summary> Gets a blank <see cref="T:Discord.GuildPermissions" /> that grants no permissions. </summary>
    public static readonly CacheGuildPermissions None = new CacheGuildPermissions();
    /// <summary> Gets a <see cref="T:Discord.GuildPermissions" /> that grants all guild permissions for webhook users. </summary>
    public static readonly CacheGuildPermissions Webhook = new CacheGuildPermissions(55296UL);
    /// <summary> Gets a <see cref="T:Discord.GuildPermissions" /> that grants all guild permissions. </summary>
    public static readonly CacheGuildPermissions All = new CacheGuildPermissions(ulong.MaxValue);
    
    public ulong RawValue { get; set; }

    public CacheGuildPermissions()
        : this(0)
    {
    }

    public CacheGuildPermissions(ulong rawValue)
    {
        RawValue = rawValue;
    }
    public CacheGuildPermissions(GuildPermissions gp)
        : this(gp.RawValue)
    {}
    public GuildPermissions ToGuildPermissions()
    {
        return new GuildPermissions(RawValue);
    }
}