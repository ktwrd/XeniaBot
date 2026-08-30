using Discord.WebSocket;

namespace XeniaBot.WebPanel.Models;

public class DiscordModel
{
    public DiscordShardedClient Client { get; set; }
}