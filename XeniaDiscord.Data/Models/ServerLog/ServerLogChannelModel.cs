using System.ComponentModel.DataAnnotations;
using XeniaDiscord.Data.Models.Cache;

namespace XeniaDiscord.Data.Models.ServerLog;

/// <summary>
/// Channel that server log events can go to.
/// </summary>
public class ServerLogChannelModel
{
    public const string TableName = "ServerLogChannel";

    public ServerLogChannelModel()
    {
        Id = Guid.NewGuid();
        GuildId = "0";
        ChannelId = "0";
        Enabled = false;

        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;

        ServerLogGuild = null!;
        GuildCache = null;
    }

    /// <summary>
    /// Record Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Guild Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId {get;set;}

    /// <summary>
    /// Channel Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string ChannelId {get;set;}

    /// <summary>
    /// What event should be sent to this channel?
    /// </summary>
    public ServerLogEvent Event { get; set; }
    /// <summary>
    /// Is this event enabled for this channel?
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// UTC Time when this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// Discord User ID that created this record. (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? CreatedByUserId { get; set; }
    /// <summary>
    /// UTC Time when this record was updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    /// <summary>
    /// Discord User ID that updated this record. (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? UpdatedByUserId { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public ServerLogGuildModel ServerLogGuild { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public GuildCacheModel? GuildCache { get; set; }
    
    public ulong GetGuildId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
    public ulong GetChannelId() => ChannelId.ParseRequiredULong(nameof(ChannelId), false);
    public ulong? GetCreatedByUserId() => CreatedByUserId.ParseULong(false);
    public ulong? GetUpdatedByUserId() => UpdatedByUserId.ParseULong(false);
}