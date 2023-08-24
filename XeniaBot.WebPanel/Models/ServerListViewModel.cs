using Discord;
using Discord.WebSocket;
using XeniaBot.Data;

namespace XeniaBot.WebPanel.Models;

public class ServerListViewModel : BaseViewModel
{
    public ulong? UserId { get; set; }
    public string? UserAvatar { get; set; }
    public ServerListViewModelItem[] Items { get; set; }
    public ListViewStyle ListStyle { get; set; }
    public ServerListViewModel()
    {
        Items = Array.Empty<ServerListViewModelItem>();
        ListStyle = ListViewStyle.List;
    }
}

public class ServerListViewModelItem
{
    public SocketGuildUser GuildUser { get; set; }
    public SocketGuild Guild { get; set; }
}