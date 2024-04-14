using Discord.WebSocket;
using XeniaBot.Data.Models;

namespace XeniaBot.WebPanel.Models.Component.FunView;

public class ServerConfessionComponentViewModel : BaseViewModel, IServerConfessionComponentViewModel
{
    public SocketGuildUser User { get; set; }
    public SocketGuild Guild { get; set; }
    
    public ConfessionGuildModel ConfessionConfig { get; set; }
}

public interface IServerConfessionComponentViewModel : IServerBaseComponentViewModel
{
    public ConfessionGuildModel ConfessionConfig { get; set; }
}