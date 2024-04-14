using Discord.WebSocket;

namespace XeniaBot.WebPanel.Models;

public interface IServerBaseComponentViewModel : IBaseViewModel
{
    public SocketGuildUser User { get; set; }
    public SocketGuild Guild { get; set; }
}