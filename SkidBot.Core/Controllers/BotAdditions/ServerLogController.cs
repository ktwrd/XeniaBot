using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SkidBot.Core.Helpers;
using SkidBot.Core.Models;
using SkidBot.Shared;

namespace SkidBot.Core.Controllers.BotAdditions;

[SkidController]
public class ServerLogController : BaseController
{
    private ServerLogConfigController _config;
    private DiscordSocketClient _discord;
    public ServerLogController(IServiceProvider services)
        : base(services)
    {
        _config = services.GetRequiredService<ServerLogConfigController>();
        _discord = services.GetRequiredService<DiscordSocketClient>();
    }

    public override Task InitializeAsync()
    {
        _discord.UserJoined += Event_UserJoined;
        _discord.UserLeft += Event_UserLeave;
        _discord.UserBanned += Event_UserBan;
        _discord.UserUnbanned += Event_UserBanRemove;

        _discord.MessageDeleted += Event_MessageDelete;
        _discord.MessageUpdated += Event_MessageEdit;

        return Task.CompletedTask;
    }
    private async Task EventHandle(ulong serverId, Func<ServerLogModel, ulong?> selectChannel, EmbedBuilder embed)
    {
        var data = await _config.Get(serverId);

        // Server not setup for logs, aborting.
        if (data == null)
            return;

        var targetChannel = selectChannel(data) ?? data.DefaultLogChannel;
        if (targetChannel == null || targetChannel == 0)
            return;

        var server = _discord.GetGuild(serverId);
        var logChannel = server.GetTextChannel(targetChannel);
        if (logChannel == null)
            return;

        await logChannel.SendMessageAsync(embed: embed.Build());
    }
    
    #region User Events
    private async Task Event_UserJoined(SocketGuildUser user)
    {
        var userSafe = user.Username.Replace("`", "\\`");
        var embed = new EmbedBuilder()
            .WithTitle("User Joined")
            .WithDescription($"<@{user.Id}>" + string.Join("\n", new string[]
            {
                "```",
                $"{userSafe}#{user.Discriminator}",
                $"ID: {user.Id}",
                "```",
            }))
            .AddField("Account Age", string.Join("\n", new string[]
            {
                TimeHelper.SinceTimestamp(user.CreatedAt.ToUnixTimeMilliseconds()),
                $"`{user.CreatedAt}`"
            }))
            .WithThumbnailUrl(user.GetAvatarUrl())
            .WithColor(Color.Green);

        await EventHandle(user.Guild.Id, (v) => v.MemberJoinChannel, embed);
    }
    private async Task Event_UserLeave(SocketGuild guild, SocketUser user)
    {
        var userSafe = user.Username.Replace("`", "\\`");
        var embed = new EmbedBuilder()
            .WithTitle("User Left")
            .WithDescription($"<@{user.Id}>" + string.Join("\n", new string[]
            {
                "```",
                $"{userSafe}#{user.Discriminator}",
                $"ID: {user.Id}",
                "```",
            }))
            .AddField("Account Age", string.Join("\n", new string[]
            {
                TimeHelper.SinceTimestamp(user.CreatedAt.ToUnixTimeMilliseconds()),
                $"`{user.CreatedAt}`"
            }))
            .WithThumbnailUrl(user.GetAvatarUrl())
            .WithColor(Color.Red);

        await EventHandle(guild.Id, (v) => v.MemberLeaveChannel, embed);
    }

    private async Task Event_UserBan(SocketUser user, SocketGuild guild)
    {
        var userSafe = user.Username.Replace("`", "\\`");
        var banDetails = await guild.GetBanAsync(user.Id);
        var reason = banDetails.Reason ?? "<Unknown Reason>";
        var embed = new EmbedBuilder()
            .WithTitle("User Banned")
            .WithDescription($"<@{user.Id}>" + string.Join("\n", new string[]
            {
                "```",
                $"{userSafe}#{user.Discriminator}",
                $"ID: {user.Id}",
                "```",
            }))
            .AddField("Account Age", string.Join("\n", new string[]
            {
                TimeHelper.SinceTimestamp(user.CreatedAt.ToUnixTimeMilliseconds()),
                $"`{user.CreatedAt}`"
            }))
            .AddField("Ban Reason", $"```\n{reason}\n```")
            .WithThumbnailUrl(user.GetAvatarUrl())
            .WithColor(Color.Red);

        await EventHandle(guild.Id, (v) => v.MemberBanChannel, embed);
    }
    private async Task Event_UserBanRemove(SocketUser user, SocketGuild guild)
    {
        var userSafe = user.Username.Replace("`", "\\`");
        var embed = new EmbedBuilder()
            .WithTitle("User Unbanned")
            .WithDescription($"<@{user.Id}>" + string.Join("\n", new string[]
            {
                "```",
                $"{userSafe}#{user.Discriminator}",
                $"ID: {user.Id}",
                "```",
            }))
            .AddField("Account Age", string.Join("\n", new string[]
            {
                TimeHelper.SinceTimestamp(user.CreatedAt.ToUnixTimeMilliseconds()),
                $"`{user.CreatedAt}`"
            }))
            .WithThumbnailUrl(user.GetAvatarUrl())
            .WithColor(Color.Red);

        await EventHandle(guild.Id, (v) => v.MemberBanChannel, embed);
    }
    #endregion
    
    #region Message Events

    private async Task Event_MessageDelete(Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel)
    {
        var socketChannel = channel.Value as SocketGuildChannel;
        var embed = new EmbedBuilder()
            .WithTitle("Message Deleted")
            .WithDescription($"Deleted in <#{channel.Id}> at <t:{message.Value.CreatedAt.ToUnixTimeMilliseconds()}:F>")
            .AddField("Content", message.Value.Content)
            .WithColor(Color.Orange);
        await EventHandle(socketChannel.Guild.Id, (v) => v.MessageDeleteChannel, embed);
    }

    private async Task Event_MessageEdit(Cacheable<IMessage, ulong> previousMessage, SocketMessage currentMessage,
        IMessageChannel channel)
    {
        var previousContent = previousMessage.Value?.Content ?? "";
        if (previousContent == currentMessage.Content)
            return;
        var socketChannel = channel as SocketGuildChannel;
        var embed = new EmbedBuilder()
            .WithTitle("Message Edited")
            .WithDescription($"From `{currentMessage.Author.Username}#{currentMessage.Author.Discriminator}`\nID: `{currentMessage.Author.Id}`")
            .AddField("Difference", string.Join("\n",
                new string[]
                {
                    "```",
                    string.Join("\n", SGeneralHelper.GenerateDifference(previousContent ?? "", currentMessage?.Content ?? "")),
                    "```",
                }))
            .WithColor(new Color(255, 255, 255))
            .WithUrl($"https://discord.com/channels/{socketChannel.Guild.Id}/{socketChannel.Id}/{currentMessage.Id}")
            .WithThumbnailUrl(currentMessage.Author.GetAvatarUrl());
        await EventHandle(socketChannel.Guild.Id, (v) => v.MessageEditChannel, embed);
    }
    #endregion
}