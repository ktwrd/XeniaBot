using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.RolePreserve;

public class RolePreserveAuditModel
{
    public const string TableName = "RolePreserveAudit";
    
    public RolePreserveAuditModel()
    {
        Id = Guid.NewGuid();
        RecordCreatedAt = DateTime.UtcNow;
        GuildId = "0";
        Action = RolePreserveAuditAction.Unknown;
    }
    
    public Guid Id { get; set; }
    
    public DateTime RecordCreatedAt { get; set; }
    
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }
    
    public RolePreserveAuditAction Action { get; set; }
    
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? UserId { get; set; }
    
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? TargetUserId { get; set; }
    
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? TargetRoleId { get; set; }
}

public enum RolePreserveAuditAction
{
    Unknown,
    BlacklistAdd,
    BlacklistRemove,
    Enable,
    Disable
}