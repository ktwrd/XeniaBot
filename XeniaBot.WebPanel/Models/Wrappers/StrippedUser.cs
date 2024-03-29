using System.Collections.Generic;
using Discord;
using Discord.WebSocket;

namespace XeniaBot.WebPanel.Models;

public class StrippedUser
{
    public string AvatarUrl { get; set; }
    public string Discriminator { get; set; }
    public string Username { get; set; }
    public string DisplayName { get; set; }
    public bool IsBot { get; set; }
    public bool IsWebhook { get; set; }
    public ulong Id { get; set; }
    
    public static IEnumerable<StrippedUser> FromGuild(DiscordSocketClient client, SocketGuild guild)
    {
        var users = new List<StrippedUser>();
        foreach (var i in guild.Users)
        {
            users.Add(StrippedUser.FromUser(client, i));
        }
        return users;
    }
    public static StrippedUser FromUser(DiscordSocketClient client, IUser user)
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