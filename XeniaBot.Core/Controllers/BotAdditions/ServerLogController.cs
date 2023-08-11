using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Controllers.Wrappers;
using XeniaBot.Core.Controllers.Wrappers.BigBrother;
using XeniaBot.Core.Helpers;
using XeniaBot.Core.Models;
using XeniaBot.Shared;

namespace XeniaBot.Core.Controllers.BotAdditions;

[BotController]
public class ServerLogController : BaseController
{
    private ServerLogConfigController _config;
    private DiscordSocketClient _discord;
    private BigBrotherController _bb;
    public ServerLogController(IServiceProvider services)
        : base(services)
    {
        _config = services.GetRequiredService<ServerLogConfigController>();
        _discord = services.GetRequiredService<DiscordSocketClient>();
        _bb = services.GetRequiredService<BigBrotherController>();
    }

    public override Task InitializeAsync()
    {
        _discord.UserJoined += Event_UserJoined;
        _discord.UserLeft += Event_UserLeave;
        _discord.UserBanned += Event_UserBan;
        _discord.UserUnbanned += Event_UserBanRemove;

        _discord.ChannelDestroyed += Event_ChannelDestroyed;
        _discord.ChannelCreated += Event_ChannelCreated;
        _discord.UserVoiceStateUpdated += Event_UserVoiceStateUpdated;

        _discord.MessageDeleted += Event_MessageDelete;
        // _discord.MessageUpdated += Event_MessageEdit;
        _bb.MessageChange += _bb_MessageChange_Update;

        return Task.CompletedTask;
    }

    
    private async void _bb_MessageChange_Update(MessageChangeType type, BB_MessageModel current, BB_MessageModel? previous)
    {
        if (type != MessageChangeType.Update)
            return;

        var previousContent = previous?.Content ?? "";
        var currentContent = current.Content ?? "";
        if (previousContent == currentContent)
            return;

        var author = _discord.GetUser(current.AuthorId);
        if (author == null)
            return;

        var embed = DiscordHelper.BaseEmbed()
            .WithTitle("Message Edited")
            .WithDescription(string.Join("\n", new string[]
            {
                $"From: `{author.Username}#{author.Discriminator}`",
                $"ID: `{current.AuthorId}`"
            }))
            .WithColor(new Color(255, 255, 255))
            .WithUrl($"https://discord.com/channels/{current.GuildId}/{current.ChannelId}/{current.Snowflake}")
            .WithThumbnailUrl(author.GetAvatarUrl())
            .AddField("Difference", string.Join("\n", new string[]
            {
                "```",
                string.Join("\n", SGeneralHelper.GenerateDifference(previousContent ?? "", currentContent ?? "")),
                "```"
            }));
        await EventHandle(current.GuildId, (v => v.MessageEditChannel), embed);
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

    private async Task Event_UserVoiceStateUpdated(
        SocketUser user,
        SocketVoiceState previous,
        SocketVoiceState current)
    {
        if (!(user is SocketGuildUser guildUser))
            return;
        
        var changeList = new List<string>();
        if (previous.IsStreaming != current.IsStreaming)
            changeList.Add(current.IsStreaming ? "+ Streaming" : "- Streaming");
        if (previous.IsVideoing != current.IsVideoing)
            changeList.Add(current.IsVideoing ? "+ Camera" : "- Camera");
        if (previous.IsSelfMuted != current.IsSelfMuted)
            changeList.Add(current.IsSelfMuted ? "+ Muted Self" : "- Muted Self");
        if (previous.IsSelfDeafened != current.IsSelfDeafened)
            changeList.Add(current.IsSelfDeafened ? "+ Deafened Self" : "- Deafened Self");
        if (previous.IsDeafened != current.IsDeafened)
            changeList.Add(current.IsDeafened ? "+ Server Deaf" : "- Server Deaf");
        if (previous.IsMuted != current.IsMuted)
            changeList.Add(current.IsMuted ? "+ Server Mute" : "- Server Mute");

        var embed = new EmbedBuilder()
            .WithTitle("User Voice State changed");
        if (changeList.Count > 0)
            embed.AddField("Changes", "```\n" + string.Join("\n", changeList) + "\n```");
        
        var currentChannel = current.VoiceChannel;
        var previousChannel = previous.VoiceChannel;
        if (currentChannel != null && previousChannel == null)
            embed.WithDescription($"Joined <#{currentChannel.Id}>");
        else if (currentChannel == null && previousChannel != null)
            embed.WithDescription($"Left <#{previous.VoiceChannel.Id}>");
        else if (currentChannel != null && previousChannel != null)
            if (currentChannel.Id != previousChannel.Id)
                embed.WithDescription($"Switched from <#{previousChannel.Id}> to <#{currentChannel.Id}>");
        
        await EventHandle(guildUser.Guild.Id, (v) => v.MemberVoiceChangeChannel, embed);
    }
    private async Task Event_ChannelDestroyed(SocketChannel channel)
    {
        if (!(channel is SocketGuildChannel guildChannel))
            return;
        var embed = new EmbedBuilder()
            .WithTitle("Channel Deleted")
            .WithDescription($"<#{channel.Id}> {guildChannel.Name}")
            .WithColor(Color.Red);
        await EventHandle(guildChannel.Guild.Id, (v) => v.ChannelDeleteChannel, embed);
    }

    private async Task Event_ChannelCreated(SocketChannel channel)
    {
        if (!(channel is SocketGuildChannel guildChannel))
            return;
        var embed = new EmbedBuilder()
            .WithTitle("Channel Created")
            .WithDescription($"<#{channel.Id}> {guildChannel.Name}")
            .WithColor(Color.Blue);
        await EventHandle(guildChannel.Guild.Id, (v) => v.ChannelCreateChannel, embed);
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
        if (socketChannel?.Guild == null)
            return;
        var funkyMessage = await _bb.BBMessageConfig.Get(message.Id);
        
        string messageContent = message.Value?.Content ?? funkyMessage?.Content ?? "";
        long timestamp = 
            message.Value?.CreatedAt.ToUnixTimeMilliseconds()
            ?? funkyMessage?.CreatedAt.ToUnixTimeMilliseconds()
            ?? 0;
        SocketUser? author = _discord.GetUser(message.Value?.Author.Id ?? funkyMessage?.AuthorId ?? 0);
        var embed = DiscordHelper.BaseEmbed()
            .WithTitle("Message Deleted")
            .WithDescription($"Deleted in <#{channel.Id}> at <t:{timestamp}:F>")
            .WithColor(Color.Orange);
        if (author != null)
            embed.WithThumbnailUrl(author.GetAvatarUrl());
        if (messageContent.Length > 0)
            embed.AddField("Content", messageContent);
        await EventHandle(socketChannel.Guild.Id, (v) => v.MessageDeleteChannel, embed);
    }

    private async Task Event_MessageEdit(Cacheable<IMessage, ulong> previousMessage, SocketMessage currentMessage,
        IMessageChannel channel)
    {
        var previousContent = previousMessage.Value?.Content ?? "";
        if (previousContent == currentMessage.Content)
            return;
        var socketChannel = channel as SocketGuildChannel;
        var embed = DiscordHelper.BaseEmbed()
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