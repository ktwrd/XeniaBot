using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;

namespace XeniaBot.WebPanel.Models;

public class StrippedUser
{
    public string AvatarUrl { get; set; } = string.Empty;
    public string Discriminator { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsBot { get; set; }
    public bool IsWebhook { get; set; }
    public ulong Id { get; set; }
    
    public static IEnumerable<StrippedUser> FromGuild(DiscordShardedClient client, SocketGuild guild)
    {
        return guild.Users
            .Select(i => FromUser(client, i))
            .ToList();
    }

    public static StrippedUser FromUser(DiscordShardedClient client, IUser user)
    {
        var i = new StrippedUser();

        i.AvatarUrl = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl();
        i.Discriminator = user.Discriminator;
        i.Username = user.Username;
        i.DisplayName = user.GlobalName;
        i.IsBot = user.IsBot;
        i.IsWebhook = user.IsWebhook;
        i.Id = user.Id;
        
        return i;
    }
}