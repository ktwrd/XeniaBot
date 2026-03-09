using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using XeniaDiscord.Data.Models.Cache;
using XeniaDiscord.Data.Models.PartialSnapshot;

namespace XeniaDiscord.Data.Models.BanSync;

public class BanSyncRecordModel
{
    public const string TableName = "BanSyncRecords";

    public BanSyncRecordModel()
    {
        Id = Guid.NewGuid();
        GuildId = "0";
        GuildName = "";
        UserId = "0";
        CreatedAt = DateTime.UtcNow;
        Source = BanSyncRecordSource.Unknown;
    }

    /// <summary>
    /// Primary Key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Guild Id (ulong as string)
    /// </summary>
    [Required]
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }

    /// <summary>
    /// Guild Name
    /// </summary>
    public string GuildName { get; set; }

    /// <summary>
    /// User Id (ulong as string)
    /// </summary>
    [Required]
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string UserId { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="UserPartialSnapshotModel.Id"/>
    /// </summary>
    public Guid UserPartialSnapshotId { get; set; }

    /// <summary>
    /// Time when this record was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Reason why this person was banned.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// User Id that banned <see cref="UserId"/> (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? BannedByUserId { get; set; }
    
    /// <summary>
    /// Pretend that this record doesn't exist when <see langword="true"/>
    /// </summary>
    [DefaultValue(false)]
    public bool Ghost { get; set; }

    public BanSyncRecordSource Source { get; set; }

    public ulong GetGuildId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
    public ulong GetUserId() => UserId.ParseRequiredULong(nameof(UserId), false);
    public ulong? GetBannedByUserId() => BannedByUserId.ParseULong(false);

    public UserPartialSnapshotModel UserPartialSnapshot { get; set; } = null!;
    public BanSyncGuildModel BanSyncGuild { get; set; } = null!;
    public GuildMemberCacheModel? CachedGuildMember { get; set; }
}

public enum BanSyncRecordSource
{
    Unknown,
    DataMigration_MongoDb,
    MemberBanEvent,
    GuildRefresh
}
