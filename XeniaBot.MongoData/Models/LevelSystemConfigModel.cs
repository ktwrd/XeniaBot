using System.Collections.Generic;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models;

public class LevelSystemConfigModel : BaseModel
{
    public ulong GuildId { get; set; }
    public ulong? LevelUpChannel { get; set; }
    public bool ShowLeveUpMessage { get; set; }
    public bool Enable { get; set; }
    
    /// <summary>
    /// Role rewards to grant user when they reach a milestone.
    /// </summary>
    public List<LevelSystemRoleGrantItem> RoleGrant { get; set; }

    public LevelSystemConfigModel()
    {
        LevelUpChannel = null;
        Enable = true;
        ShowLeveUpMessage = true;
        RoleGrant = new List<LevelSystemRoleGrantItem>();
    }
}

public class LevelSystemRoleGrantItem
{
    /// <summary>
    /// RoleId to grant to member when they reach the calculated level of <see cref="RequiredLevel"/>.
    /// </summary>
    public ulong RoleId { get; set; }
    /// <summary>
    /// Minimum required level to have <see cref="RoleId"/> granted to them.
    /// </summary>
    public ulong RequiredLevel { get; set; }
}