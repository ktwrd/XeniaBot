using Discord;
using Discord.WebSocket;

namespace XeniaBot.Data.Helpers;

public static class DataHelper
{
    
    /// <summary>
    /// Check if <paramref name="channel"/> has the ViewChannel, SendMessages, and EmbedLinks permissions.
    /// </summary>
    /// <returns>If <paramref name="client"/> has access to <paramref name="channel"/></returns>
    public static bool CanAccessChannel(DiscordSocketClient client, SocketGuildChannel channel)
    {
        var currentGuildUser = channel.Guild.GetUser(client.CurrentUser.Id);
        var channelPermissions = currentGuildUser.GetPermissions(channel);
        return channelPermissions.Has(ChannelPermission.ViewChannel) &&
               channelPermissions.Has(ChannelPermission.SendMessages) &&
               channelPermissions.Has(ChannelPermission.EmbedLinks) &&
               channelPermissions.Has(ChannelPermission.ReadMessageHistory);
    }
}