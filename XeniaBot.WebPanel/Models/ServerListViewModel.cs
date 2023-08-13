using Discord;
using Discord.WebSocket;

namespace XeniaBot.WebPanel.Models;

public class ServerListViewModel
{
    public ulong? UserId { get; set; }
    public string? UserAvatar { get; set; }
    public ServerListViewModelItem[] Items { get; set; }

    public ServerListViewModel()
    {
        Items = Array.Empty<ServerListViewModelItem>();
    }
}

public class ServerListViewModelItem
{
    public SocketGuildUser GuildUser { get; set; }
    public SocketGuild Guild { get; set; }
}