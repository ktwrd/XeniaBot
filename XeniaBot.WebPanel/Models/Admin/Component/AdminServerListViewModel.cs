using System.Collections.Generic;
using System.Linq;
using Discord.WebSocket;

namespace XeniaBot.WebPanel.Models.Component;

public class AdminServerListViewModel
{
    public IEnumerable<SocketGuild> Guilds { get; set; }
    public int Cursor { get; set; }
    public bool IsLastPage => Guilds.Count() < PageSize;
    public const int PageSize = 10;

    public bool IsGuildLast(SocketGuild guild)
    {
        if (Guilds.Count() < 2)
            return true;

        return Guilds.ElementAt(Guilds.Count() - 1).Id == guild.Id;
    }
}