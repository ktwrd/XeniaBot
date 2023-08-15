using Discord.WebSocket;

namespace XeniaBot.WebPanel.Models;

public class AdminIndexModel
{
    public IEnumerable<SocketGuild> Guilds { get; set; }
}