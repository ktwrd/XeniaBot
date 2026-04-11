using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.RolePreserve;

public class RolePreserveUserRoleModel
{
    public const string TableName = "RolePreserveUserRoles";

    public RolePreserveUserRoleModel()
    {
        GuildId = "0";
        UserId = "0";
        RoleId = "0";
    }

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
    /// Role Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string RoleId { get; set; }

    public ulong GetGuildId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
    public ulong GetUserId() => UserId.ParseRequiredULong(nameof(UserId), false);
    public ulong GetRoleId() => RoleId.ParseRequiredULong(nameof(RoleId), false);
}
