using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.BanSync;

public class BanSyncRecordModel
{
    public const string TableName = "BanSyncRecords";

    public BanSyncRecordModel()
    {
        Id = Guid.NewGuid();
        GuildId = "0";
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
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }

    /// <summary>
    /// User Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string UserId { get; set; }

    /// <summary>
    /// Time when this record was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Reason why this person was banned.
    /// </summary>
    public string? Reason { get; set; }
    
    /// <summary>
    /// Pretend that this record doesn't exist when <see langword="true"/>
    /// </summary>
    [DefaultValue(false)]
    public bool Ghost { get; set; }

    public BanSyncRecordSource Source { get; set; }


    public ulong GetGuildId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
    public ulong GetUserId() => UserId.ParseRequiredULong(nameof(UserId), false);
}

public enum BanSyncRecordSource
{
    Unknown,
    MongodbImport,
    MemberBanEvent,
    GuildRefresh
}
