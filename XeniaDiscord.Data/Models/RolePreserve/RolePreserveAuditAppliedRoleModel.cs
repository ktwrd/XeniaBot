using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using XeniaDiscord.Data.Models.Cache;

namespace XeniaDiscord.Data.Models.RolePreserve;

public class RolePreserveAuditAppliedRoleModel
{
    public const string TableName = "RolePreserveAudit_AppliedRoles";
    public RolePreserveAuditAppliedRoleModel()
    {
        RolePreserveAuditId = Guid.Empty;
        RoleId = "0";
        Action = RolePreserveAuditAppliedRoleAction.FailureUnknown;
    }

    /// <summary>
    /// Foreign Key to <see cref="RolePreserveAuditModel.Id"/>
    /// </summary>
    public Guid RolePreserveAuditId { get; set; }

    /// <summary>
    /// Role Id that was applied, or Xenia tried to (ulong as string)
    /// Optional foreign key to <see cref="GuildRoleCacheModel.RoleId"/>
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string RoleId { get; set; }
    
    /// <summary>
    /// Action in relation to the role
    /// </summary>
    public RolePreserveAuditAppliedRoleAction Action { get; set; }

    /// <summary>
    /// Only used if <see cref="Action"/> is <see cref="RolePreserveAuditAppliedRoleAction.FailureUnknown"/>
    /// </summary>
    public string? ExceptionText { get; set; }

    public ulong GetRoleId() => RoleId.ParseRequiredULong(nameof(RoleId), false);

    public bool IsActionFailure()
        => Action == RolePreserveAuditAppliedRoleAction.FailureUnknown
        || Action == RolePreserveAuditAppliedRoleAction.FailureMissingPermissions
        || Action == RolePreserveAuditAppliedRoleAction.FailureMissingPermissionsHierarchy;
    
    public bool IsActionSkip()
        => Action == RolePreserveAuditAppliedRoleAction.SkippedRoleDoesNotExist
        || Action == RolePreserveAuditAppliedRoleAction.SkippedBlacklisted;
    
    public bool IsActionSuccess()
        => Action == RolePreserveAuditAppliedRoleAction.SuccessGrant;
    
    
    /// <summary>
    /// Property Accessor
    /// </summary>
    public GuildRoleCacheModel? Role { get; set; }
}

public enum RolePreserveAuditAppliedRoleAction
{
    [Description("Failure - Unknown")]
    FailureUnknown = 0,
    [Description("Success - Granted")]
    SuccessGrant,
    [Description("Skipped - Role does not exist")]
    SkippedRoleDoesNotExist,
    [Description("Skipped - Blacklisted")]
    SkippedBlacklisted,
    [Description("Failed to apply - Missing permissions")]
    FailureMissingPermissions,
    [Description("Failed to apply - Missing permissions (hierarchy issue)")]
    FailureMissingPermissionsHierarchy
}