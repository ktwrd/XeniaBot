using System.ComponentModel.DataAnnotations;
using XeniaDiscord.Data.Models.Cache;

namespace XeniaDiscord.Data.Models.RolePreserve;

/// <summary>
/// Used as a reference to a role when adding/removing from blacklist
/// </summary>
public class RolePreserveAuditReferencedRoleModel
{
    public const string TableName = "RolePreserveAudit_ReferencedRoles";

    /// <summary>
    /// Foreign Key to <see cref="RolePreserveAuditModel.Id"/>
    /// </summary>
    public Guid RolePreserveAuditId { get; set; } = Guid.Empty;

    /// <summary>
    /// Role Id that was applied, or Xenia tried to (ulong as string)
    /// Optional foreign key to <see cref="GuildRoleCacheModel.RoleId"/>
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string RoleId { get; set; } = "0";
    
    /// <summary>
    /// Property Accessor
    /// </summary>
    public GuildRoleCacheModel? Role { get; set; }
}