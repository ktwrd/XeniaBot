using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.RolePreserve;

public class RolePreserveUserModel
{
    public const string TableName = "RolePreserveUsers";

    public RolePreserveUserModel()
    {
        GuildId = "0";
        UserId = "0";
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
        RolePreserveGuild = null!;
        Roles = new List<RolePreserveUserRoleModel>();
    }

    /// <summary>
    /// <para>Guild Id (ulong as string)</para>
    /// Also a foreign key to <see cref="RolePreserveGuildModel.GuildId"/>
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }
    
    /// <summary>
    /// User Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string UserId { get; set; }
    
    /// <summary>
    /// When this record was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// When this record was updated (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Property Accessor
    /// </summary>
    public RolePreserveGuildModel RolePreserveGuild { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public List<RolePreserveUserRoleModel> Roles { get; set; }

    public ulong GetGuildId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
    public ulong GetUserId() => UserId.ParseRequiredULong(nameof(UserId), false);
}