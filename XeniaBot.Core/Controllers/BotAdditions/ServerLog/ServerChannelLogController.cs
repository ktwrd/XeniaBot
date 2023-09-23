using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Controllers.Wrappers;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Models.Archival;
using XeniaBot.DiscordCache.Models;
using XeniaBot.Shared;

namespace XeniaBot.Core.Controllers.BotAdditions;

[BotController]
public class ServerChannelLogController : BaseController
{
    private readonly ServerLogConfigController _config;
    private readonly DiscordSocketClient _discord;
    private readonly DiscordCacheController _discordCache;
    private readonly ServerLogController _serverLog;

    public ServerChannelLogController(IServiceProvider services)
        : base(services)
    {
        _config = services.GetRequiredService<ServerLogConfigController>();
        _discord = services.GetRequiredService<DiscordSocketClient>();
        _discordCache = services.GetRequiredService<DiscordCacheController>();
        _serverLog = services.GetRequiredService<ServerLogController>();
    }

    public override Task InitializeAsync()
    {
        _discord.ChannelDestroyed += _discord_ChannelDestroyed;
        _discord.ChannelCreated += _discord_ChannelCreated;
        _discordCache.ChannelChange += _discordCache_ChannelChange;
        _discord.UserVoiceStateUpdated += _discord_UserVoiceStateUpdated;

        return Task.CompletedTask;
    }

    private async void _discordCache_ChannelChange(CacheChangeType changeType, CacheGuildChannelModel current, CacheGuildChannelModel? previous)
    {
        if (current.Name != previous?.Name)
        {
            await _serverLog.EventHandle(current.Guild.Snowflake, (v) => v.ChannelEditChannel, new EmbedBuilder()
                .WithTitle("Channel Name Changed")
                .WithDescription($"<#{current.Snowflake}> changed to `{current.Name}` from `{previous.Name}`")
                .WithCurrentTimestamp()
                .WithColor(Color.Blue));
        }
    }
    
    private async Task _discord_ChannelDestroyed(SocketChannel channel)
    {
        if (!(channel is SocketGuildChannel guildChannel))
            return;
        var embed = new EmbedBuilder()
            .WithTitle("Channel Deleted")
            .WithDescription($"<#{channel.Id}> {guildChannel.Name}")
            .WithColor(Color.Red);
        await _serverLog.EventHandle(guildChannel.Guild.Id, (v) => v.ChannelDeleteChannel, embed);
    }

    private async Task _discord_ChannelCreated(SocketChannel channel)
    {
        if (!(channel is SocketGuildChannel guildChannel))
            return;
        var embed = new EmbedBuilder()
            .WithTitle("Channel Created")
            .WithDescription($"<#{channel.Id}> {guildChannel.Name}")
            .WithColor(Color.Blue);
        await _serverLog.EventHandle(guildChannel.Guild.Id, (v) => v.ChannelCreateChannel, embed);
    }
    private async Task _discord_UserVoiceStateUpdated(
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
        
        await _serverLog.EventHandle(guildUser.Guild.Id, (v) => v.MemberVoiceChangeChannel, embed);
    }
}