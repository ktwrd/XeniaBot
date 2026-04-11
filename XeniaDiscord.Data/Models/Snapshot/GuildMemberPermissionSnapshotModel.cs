using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Snapshot;

public class GuildMemberPermissionSnapshotModel : IGuildMemberPermissionSnapshot
{
    public const string TableName = "Snapshot_GuildMemberPermission";
    public GuildMemberPermissionSnapshotModel()
    {
        RecordId = Guid.NewGuid();
        RecordCreatedAt = DateTime.UtcNow;
        GuildMemberSnapshotId = Guid.Empty;
        UserId = "0";
        GuildId = "0";
        Value = "0";
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
    public string Value { get; set; }

    /// <inheritdoc/>
    public ulong GetUserId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
    /// <inheritdoc/>
    public ulong GetGuildId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
    /// <inheritdoc/>
    public Discord.GuildPermission GetValue()
    {
        if (ulong.TryParse(Value, out var number))
        {
            return (Discord.GuildPermission)number;
        }
        throw new InvalidOperationException($"Failed to convert value \"{Value}\" (as ulong) to {typeof(Discord.GuildPermission)}");
    }
}

public interface IGuildMemberPermissionSnapshot : ISnapshot
{
    /// <summary>
    /// Foreign Key to <see cref="IGuildMemberSnapshot.RecordId"/>
    /// </summary>
    public Guid GuildMemberSnapshotId { get; set; }

    /// <summary>
    /// User Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string UserId { get; set; }
    /// <summary>
    /// Guild Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }

    /// <summary>
    /// Singular flag of <see cref="Discord.GuildPermission"/>
    /// (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string Value { get; set; }

    public ulong GetUserId();
    public ulong GetGuildId();
    /// <summary>
    /// Parse <see cref="Value"/> to <see cref="Discord.GuildPermission"/> via <see cref="ulong"/>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="Value"/> could not be parsed as a <see cref="ulong"/>
    /// </exception>
    public Discord.GuildPermission GetValue();
}