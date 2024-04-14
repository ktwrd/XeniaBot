using Discord.WebSocket;
using XeniaBot.Data.Models;

namespace XeniaBot.WebPanel.Models.Component.FunView;

public class ServerCountingComponentViewModel : BaseViewModel, IServerCountingComponentViewModel
{
    public SocketGuildUser User { get; set; }
    public SocketGuild Guild { get; set; }
    
    public CounterGuildModel CounterConfig { get; set; }
}

public interface IServerCountingComponentViewModel : IServerBaseComponentViewModel
{
    public CounterGuildModel CounterConfig { get; set; }
}