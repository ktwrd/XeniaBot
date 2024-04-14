using Discord.WebSocket;
using XeniaBot.Data.Models;

namespace XeniaBot.WebPanel.Models.Component.FunView;

public class ServerConfessionComponentViewModel : BaseViewModel
{
    public SocketGuildUser User { get; set; }
    public SocketGuild Guild { get; set; }
    
    public ConfessionGuildModel ConfessionConfig { get; set; }
}