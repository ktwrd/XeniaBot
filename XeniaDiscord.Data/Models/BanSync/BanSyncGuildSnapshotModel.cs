using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.BanSync;

public class BanSyncGuildSnapshotModel : IBanSyncGuildModel
{
    public const string TableName = "BanSyncGuildSnapshot";

    public BanSyncGuildSnapshotModel()
    {
        Timestamp = DateTime.UtcNow;
        Id = "0";
        LogChannelId = "0";
        CreatedAt = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    [MaxLength(DbGlobals.UnsignedLongStringLength)]
    public string Id { get; set; }

    /// <inheritdoc/>
    [MaxLength(DbGlobals.MaxLength.Discord.GuildName)]
    public string? GuildName { get; set; }

    /// <inheritdoc/>
    [MaxLength(DbGlobals.UnsignedLongStringLength)]
    public string? LogChannelId { get; set; }

    /// <inheritdoc/>
    [DefaultValue(false)]
    public bool Enabled { get; set; }

    /// <inheritdoc/>
    public BanSyncGuildState State { get; set; }

    /// <inheritdoc/>
    [MaxLength(4000)]
    public string? InternalNote { get; set; }

    /// <inheritdoc/>
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc/>
    public DateTime? UpdatedAt { get; set; }

    /// <inheritdoc/>
    public string? UpdatedByUserId { get; set; }
}
