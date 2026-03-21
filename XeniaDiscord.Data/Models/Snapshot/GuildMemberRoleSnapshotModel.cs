using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Snapshot;

public class GuildMemberRoleSnapshotModel : IGuildMemberRoleSnapshot
{
    public const string TableName = "Snapshot_GuildMemberRole";
    public GuildMemberRoleSnapshotModel()
    {
        RecordId = Guid.NewGuid();
        RecordCreatedAt = DateTime.UtcNow;
        GuildMemberSnapshotId = Guid.Empty;
        UserId = "0";
        GuildId = "0";
        RoleId = "0";
    }

    /// <inheritdoc/>
    public Guid RecordId { get; set; }
    /// <inheritdoc/>
    public Guid GuildMemberSnapshotId { get; set; }
    /// <inheritdoc/>
    public DateTime RecordCreatedAt { get; set; }
    /// <inheritdoc/>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string UserId { get; set; }
    /// <inheritdoc/>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }
    /// <inheritdoc/>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string RoleId { get; set; }

    /// <inheritdoc/>
    public ulong GetUserId() => UserId.ParseRequiredULong(nameof(UserId), false);
    /// <inheritdoc/>
    public ulong GetGuildId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
    /// <inheritdoc/>
    public ulong GetRoleId() => RoleId.ParseRequiredULong(nameof(RoleId), false);
}

public interface IGuildMemberRoleSnapshot : ISnapshot
{
    public Guid GuildMemberSnapshotId { get; set; }

    [MaxLength(DbGlobals.ulongMaxLength)]
    public string UserId { get; set; }

    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }

    [MaxLength(DbGlobals.ulongMaxLength)]
    public string RoleId { get; set; }

    public ulong GetUserId();
    public ulong GetGuildId();
    public ulong GetRoleId();
}