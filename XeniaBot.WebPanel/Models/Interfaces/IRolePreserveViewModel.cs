using XeniaDiscord.Data.Models.RolePreserve;

namespace XeniaBot.WebPanel.Models;

public interface IRolePreserveViewModel
{
    public RolePreserveGuildModel RolePreserve { get; set; }
}