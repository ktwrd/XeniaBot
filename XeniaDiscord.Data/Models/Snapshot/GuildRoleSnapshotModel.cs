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
    public GuildRoleSnapshotModel(IRole role)
    {
        GuildId = role.Guild.Id.ToString();
        RoleId = role.Id.ToString();
        Name = role.Name;
        CreatedAt = role.CreatedAt.UtcDateTime;
        PermissionsValue = role.Permissions.RawValue.ToString();
        Position = role.Position;
        IconHash = string.IsNullOrEmpty(role.Icon) ? null : role.Icon;
        IsManaged = role.IsManaged;
        IsMentionable = role.IsMentionable;
        IsHoisted = role.IsHoisted;
        RecordCreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; set; }

    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }

    [MaxLength(DbGlobals.ulongMaxLength)]
    public string RoleId { get; set; }

    public string Name { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// <see cref="GuildPermissions.RawValue"/> stored as a string (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string PermissionsValue { get; set; }

    public int Position { get; set; }

    public RoleFlags Flags { get; set; }

    public string? IconHash { get; set; }

    public bool IsManaged { get; set; }
    public bool IsMentionable { get; set; }
    public bool IsHoisted { get; set; }

    /// <summary>
    /// UTC Time of when this record was created.
    /// </summary>
    public DateTime RecordCreatedAt { get; set; }

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
