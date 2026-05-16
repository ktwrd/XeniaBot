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
        AppliedRoles = new();
    }
    
    /// <summary>
    /// Record ID, primary key
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// UTC Date Time of when this record was created
    /// </summary>
    public DateTime RecordCreatedAt { get; set; }
    
    /// <summary>
    /// Guild Id (ulong as string)
    /// Also a foreign key to <see cref="RolePreserveGuildModel.GuildId"/>
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }
    
    /// <summary>
    /// Action that was done
    /// </summary>
    public RolePreserveAuditAction Action { get; set; }
    
    /// <summary>
    /// User Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? UserId { get; set; }
    
    /// <summary>
    /// User Id that the action was performed on (ulong as string, currently not used)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? TargetUserId { get; set; }
    
    /// <summary>
    /// Role Id that the action was performed on (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? TargetRoleId { get; set; }
    
    /// <summary>
    /// Property Accessor
    /// </summary>
    public List<RolePreserveAuditAppliedRoleModel> AppliedRoles { get; set; }

    public ulong GetGuildId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
    public ulong? GetUserId() => UserId.ParseULong(false);
    public ulong? GetTargetUserId() => TargetUserId.ParseULong(false);
    public ulong? GetTargetRoleId() => TargetRoleId.ParseULong(false);
}

public enum RolePreserveAuditAction
{
    Unknown,
    BlacklistAdd,
    BlacklistRemove,
    Enable,
    Disable,
    AppliedRoles
}