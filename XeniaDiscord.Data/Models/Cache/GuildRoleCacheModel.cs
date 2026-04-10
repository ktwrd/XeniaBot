using System.ComponentModel.DataAnnotations;
using XeniaDiscord.Data.Models.Snapshot;

namespace XeniaDiscord.Data.Models.Cache;

// TODO add in XeniaDbContext
public class GuildRoleCacheModel
{
    public const string TableName = "Cache_GuildRole";
    public GuildRoleCacheModel()
    {
        GuildId = "0";
        RoleId = "0";
        RecordCreatedAt = DateTime.UtcNow;
        RecordUpdatedAt = RecordCreatedAt;
        SnapshotId = Guid.Empty;
        Snapshot = null!;
    }

    /// <summary>
    /// Guild Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }

    /// <summary>
    /// Role Id (ulong as string, Primary Key)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string RoleId { get; set; }

    /// <summary>
    /// UTC Date Time of when this record was created.
    /// </summary>
    public DateTime RecordCreatedAt { get; set; }

    /// <summary>
    /// UTC Date Time of when this record was last updated.
    /// </summary>
    public DateTime RecordUpdatedAt { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="GuildRoleSnapshotModel.Id"/>
    /// </summary>
    public Guid SnapshotId { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public GuildRoleSnapshotModel Snapshot { get; set; }

    public ulong GetGuildId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
    public ulong GetRoleId() => RoleId.ParseRequiredULong(nameof(RoleId), false);
}
