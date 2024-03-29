using System.Collections.Generic;
using Discord.WebSocket;

namespace XeniaBot.WebPanel.Models;

public class AdminIndexModel : BaseViewModel
{
    public IEnumerable<SocketGuild> Guilds { get; set; }
}