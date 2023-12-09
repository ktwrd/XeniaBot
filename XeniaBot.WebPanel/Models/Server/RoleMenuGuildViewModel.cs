using Discord.WebSocket;
using XeniaBot.Data.Models;

namespace XeniaBot.WebPanel.Models;

public class RoleMenuGuildViewModel : BaseViewModel
{
    public SocketGuildUser User { get; set; }
    public SocketGuild Guild { get; set; }
    
    public List<RoleMenuConfigModel> RoleMenus { get; set; }
    public List<RoleMenuSelectConfigModel> RoleSelects { get; set; }
    public List<RoleMenuOptionConfigModel> RoleOptions { get; set; }
}