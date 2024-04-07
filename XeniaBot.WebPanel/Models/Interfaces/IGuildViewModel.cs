using Discord.WebSocket;

namespace XeniaBot.WebPanel.Models;

public interface IGuildViewModel
{
    public SocketGuild Guild { get; set; }
}