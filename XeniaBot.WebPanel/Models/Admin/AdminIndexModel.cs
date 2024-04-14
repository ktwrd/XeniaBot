using System.Collections.Generic;
using Discord.WebSocket;
using XeniaBot.Data.Models;

namespace XeniaBot.WebPanel.Models;

public class AdminIndexModel : BaseViewModel
{
    public IEnumerable<SocketGuild> Guilds { get; set; }
}