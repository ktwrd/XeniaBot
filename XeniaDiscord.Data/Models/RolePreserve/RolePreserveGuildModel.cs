using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.RolePreserve;

public class RolePreserveGuildModel
{
    public const string TableName = "RolePreserveGuilds";

    public RolePreserveGuildModel()
    {
        GuildId = "0";
        Enabled = false;
        Users = [];
    }

    public RolePreserveGuildModel(
        ulong guildId) : this()
    {
        GuildId = guildId.ToString();
    }
    
    /// <summary>
    /// Guild Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }
    
    /// <summary>
    /// Is Role Preservation enabled?
    /// </summary>
    public bool Enabled { get; set; }
    
    /// <summary>
    /// Property Accessor
    /// </summary>
    public List<RolePreserveUserModel> Users { get; set; }
    
    public ulong GetGuildId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
}