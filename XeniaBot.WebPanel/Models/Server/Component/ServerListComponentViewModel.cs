using System.Collections.Generic;
using System.Linq;
using Discord.WebSocket;
using XeniaBot.Data;

namespace XeniaBot.WebPanel.Models.Component;

public class ServerListComponentViewModel : BaseViewModel
{
    public IEnumerable<ServerListViewModelItem> Items { get; set; }
    public int Cursor { get; set; }
    public bool IsLastPage => Items.Count() < PageSize;
    public ListViewStyle ListStyle { get; set; }
    public const int PageSize = 10;

    public bool IsGuildLast(ServerListViewModelItem guild)
    {
        if (Items.Count() < 2)
            return true;

        return Items.ElementAt(Items.Count() - 1).Guild.Id == guild.Guild.Id;
    }
}