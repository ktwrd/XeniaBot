using Discord;
using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Snapshot;

public class GuildRoleSnapshotModel
{
    public const string TableName = "Snapshot_GuildRole";

    public GuildRoleSnapshotModel()
    {
        Id = Guid.NewGuid();
        RecordCreatedAt = DateTime.UtcNow;
        GuildId = "0";
        RoleId = "0";
        Name = "";
        CreatedAt = DateTimeOffset.UnixEpoch.UtcDateTime;
        Position = 0;
        Permissions = [];
    }

    /// <summary>
    /// Primary Key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// UTC Time of when this record was created.
    /// </summary>
    public DateTime RecordCreatedAt { get; set; }

    /// <summary>
    /// Source of how this record was created.
    /// </summary>
    public GuildRoleSnapshotSource SnapshotSource { get; set; }

    /// <summary>
    /// Guild Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }

    /// <summary>
    /// Role Id (ulong as string)
    /// </summary>
    /// <remarks>
    /// From <see cref="IRole.Id"/>
    /// </remarks>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string RoleId { get; set; }

    /// <summary>
    /// Role Name
    /// </summary>
    /// <remarks>
    /// From <see cref="IRole.Name"/>
    /// </remarks>
    [MaxLength(200)]
    public string? Name { get; set; }

    /// <summary>
    /// UTC Time of when this role was created (inferred from snowflake)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Position of the role
    /// </summary>
    /// <remarks>
    /// From <see cref="IRole.Position"/>
    /// </remarks>
    public int Position { get; set; }

    /// <summary>
    /// Role flags
    /// </summary>
    /// <remarks>
    /// From <see cref="IRole.Flags"/>
    /// </remarks>
    public RoleFlags Flags { get; set; }

    // TODO find the actual max length. idk what it is
    /// <remarks>
    /// From <see cref="IRole.Icon"/>
    /// </remarks>
    [MaxLength(200)]
    public string? IconHash { get; set; }

    /// <remarks>
    /// From <see cref="IRole.IsManaged"/>
    /// </remarks>
    public bool IsManaged { get; set; }

    /// <remarks>
    /// From <see cref="IRole.IsMentionable"/>
    /// </remarks>

    public bool IsMentionable { get; set; }

    /// <remarks>
    /// From <see cref="IRole.IsHoisted"/>
    /// </remarks>
    public bool IsHoisted { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public List<GuildRolePermissionSnapshotModel> Permissions { get; set; }

    public ulong GetGuildId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
    public ulong GetRoleId() => RoleId.ParseRequiredULong(nameof(RoleId), false);
}

public enum GuildRoleSnapshotSource
{
    Unknown = 0,
    RoleCreate,
    RoleEdit,
    RoleDelete
}