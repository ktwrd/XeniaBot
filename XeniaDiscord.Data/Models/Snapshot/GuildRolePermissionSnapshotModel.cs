using Discord;
using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Snapshot;

public class GuildRolePermissionSnapshotModel
{
    public const string TableName = "Snapshot_GuildRolePermission";

    public GuildRolePermissionSnapshotModel()
    {
        RecordId = Guid.NewGuid();
        GuildRoleSnapshotId = Guid.Empty;
        RecordCreatedAt = DateTime.UtcNow;
        GuildId = "0";
        RoleId = "0";
        Value = "0";
    }

    /// <summary>
    /// Primary Key
    /// </summary>
    public Guid RecordId { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="GuildRoleSnapshotModel.Id"/>
    /// </summary>
    public Guid GuildRoleSnapshotId { get; set; }

    /// <summary>
    /// UTC Time of when this record was created
    /// </summary>
    public DateTime RecordCreatedAt { get; set; }
    
    /// <summary>
    /// Guild Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }
    
    /// <summary>
    /// Role Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string RoleId { get; set; }

    /// <summary>
    /// <see cref="GuildPermissions.RawValue"/> stored as a string (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string Value { get; set; }

    public ulong GetGuildId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
    public ulong GetRoleId() => RoleId.ParseRequiredULong(nameof(RoleId), false);
    public GuildPermission GetValue()
    {
        if (ulong.TryParse(Value, out var number))
        {
            return (GuildPermission)number;
        }
        throw new InvalidOperationException($"Failed to convert value \"{Value}\" (as ulong) to {typeof(Discord.GuildPermission)}");
    }
}
