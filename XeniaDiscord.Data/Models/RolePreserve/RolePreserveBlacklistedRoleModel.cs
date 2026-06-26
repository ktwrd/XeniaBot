using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.RolePreserve;

public class RolePreserveBlacklistedRoleModel
{
    public const string TableName = "RolePreserveBlacklistedRoles";

    public RolePreserveBlacklistedRoleModel()
    {
        GuildId = "0";
        RoleId = "0";
        CreatedAt = DateTime.UtcNow;
    }
    
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
    /// Discord User Id that added the role to the blacklist. (optional, ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? CreatedByUserId { get; set; }
    
    /// <summary>
    /// UTC Time of when this blacklist was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    public ulong GetGuildId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
    public ulong GetRoleId() => RoleId.ParseRequiredULong(nameof(RoleId), false);
    public ulong? GetCreatedByUserId() => CreatedByUserId.ParseULong(false);
}
