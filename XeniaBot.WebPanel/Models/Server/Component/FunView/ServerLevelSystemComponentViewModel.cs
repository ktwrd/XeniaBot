using Discord.WebSocket;
using XeniaBot.Data.Models;

namespace XeniaBot.WebPanel.Models.Component.FunView;

public class ServerLevelSystemComponentViewModel : BaseViewModel, IServerBaseComponentViewModel, IServerLevelSystemComponentViewModel
{
    public SocketGuildUser User { get; set; }
    public SocketGuild Guild { get; set; }
    
    public LevelSystemConfigModel LevelSystemConfig { get; set; }
}

public interface IServerLevelSystemComponentViewModel : IServerBaseComponentViewModel
{
    public LevelSystemConfigModel LevelSystemConfig { get; set; }
}