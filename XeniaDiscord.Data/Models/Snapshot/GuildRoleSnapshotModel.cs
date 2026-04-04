using Discord;
using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Snapshot;

public class GuildRoleSnapshotModel
{
    public const string TableName = "Snapshot_GuildRole";

    public GuildRoleSnapshotModel()
    {
        Id = Guid.NewGuid();
        GuildId = "0";
        RoleId = "0";
        Name = "";
        CreatedAt = DateTimeOffset.UnixEpoch.UtcDateTime;
        PermissionsValue = "0";
        Position = 0;
        RecordCreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; set; }

    /// <summary>
    /// UTC Time of when this record was created.
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
    /// Role Name
    /// </summary>
    [MaxLength(200)]
    public string? Name { get; set; }

    /// <summary>
    /// UTC Time of when this role was created (inferred from snowflake)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// <see cref="GuildPermissions.RawValue"/> stored as a string (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string PermissionsValue { get; set; }

    public int Position { get; set; }

    public RoleFlags Flags { get; set; }

    // TODO find the actual max length. idk what it is
    [MaxLength(200)]
    public string? IconHash { get; set; }

    public bool IsManaged { get; set; }
    public bool IsMentionable { get; set; }
    public bool IsHoisted { get; set; }

    public ulong GetGuildId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
    public ulong GetRoleId() => RoleId.ParseRequiredULong(nameof(RoleId), false);
    public GuildPermission GetValue()
    {
        if (ulong.TryParse(PermissionsValue, out var number))
        {
            return (GuildPermission)number;
        }
        throw new InvalidOperationException($"Failed to convert value \"{PermissionsValue}\" (as ulong) to {typeof(Discord.GuildPermission)}");
    }
}
