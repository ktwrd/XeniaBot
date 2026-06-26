using Discord.WebSocket;
using XeniaBot.MongoData;

namespace XeniaBot.WebPanel.Models;

public class ServerListViewModel : BaseViewModel
{
    public ulong? UserId { get; set; }
    public string? UserAvatar { get; set; }
    public ServerListViewModelItem[] Items { get; set; } = [];
    public ListViewStyle ListStyle { get; set; } = ListViewStyle.List;
}

public class ServerListViewModelItem
{
    public required SocketGuildUser GuildUser { get; set; }
    public required SocketGuild Guild { get; set; }
}