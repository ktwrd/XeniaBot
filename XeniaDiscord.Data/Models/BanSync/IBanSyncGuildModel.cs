using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.BanSync;

public interface IBanSyncGuildModel
{
    /// <summary>
    /// Guild Id (Snowflake, ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.UnsignedLongStringLength)]
    public string Id { get; set; }

    /// <summary>
    /// Guild Name. Updated when the log channel has been updated
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.Discord.GuildName)]
    public string? GuildName { get; set; }

    /// <summary>
    /// Channel Id for logging (Snowflake, ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.UnsignedLongStringLength)]
    public string? LogChannelId { get; set; }

    /// <summary>
    /// Is Ban Sync enabled?
    /// </summary>
    [DefaultValue(false)]
    public bool Enabled { get; set; }

    /// <summary>
    /// State of the guild.
    /// </summary>
    public BanSyncGuildState State { get; set; }

    /// <summary>
    /// Internal note (for bot developer only)
    /// </summary>
    [MaxLength(4000)]
    public string? InternalNote { get; set; }

    /// <summary>
    /// UTC Time when this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// UTC Time when this record was updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Discord User that updated this record (Snowflake, ulong as string)
    /// </summary>
    public string? UpdatedByUserId { get; set; }
}
