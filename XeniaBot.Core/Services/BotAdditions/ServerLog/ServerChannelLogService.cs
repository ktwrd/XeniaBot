using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Services.Wrappers;
using XeniaBot.Data.Models.Archival;
using XeniaBot.Data.Repositories;
using XeniaBot.DiscordCache.Models;
using XeniaBot.Shared;

namespace XeniaBot.Core.Services.BotAdditions;

[XeniaController]
public class ServerChannelLogService : BaseService
{
    private readonly ServerLogRepository _config;
    private readonly DiscordSocketClient _discord;
    private readonly DiscordCacheService _discordCache;
    private readonly ServerLogService _serverLog;

    public ServerChannelLogService(IServiceProvider services)
        : base(services)
    {
        _config = services.GetRequiredService<ServerLogRepository>();
        _discord = services.GetRequiredService<DiscordSocketClient>();
        _discordCache = services.GetRequiredService<DiscordCacheService>();
        _serverLog = services.GetRequiredService<ServerLogService>();
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
        if (changeType != CacheChangeType.Update)
            return;
        if (current.Name != previous?.Name)
        {
            string previousName = previous?.Name ?? "<null>";
            if (previousName.Length < 1)
                previousName = "<empty>";
            await _serverLog.EventHandle(current.Guild.Snowflake, (v) => v.ChannelEditChannel, new EmbedBuilder()
                .WithTitle("Channel Name Changed")
                .WithDescription($"<#{current.Snowflake}> changed to `{current.Name}` from `{previousName}`")
                .WithCurrentTimestamp()
                .WithColor(Color.Blue));
        }

		/*
        if (current.Position != previous?.Position)
        {
            await _serverLog.EventHandle(current.Guild.Snowflake, (v) => v.ChannelEditChannel, new EmbedBuilder()
                .WithTitle("Channel Position changed")
                .WithDescription($"<#{current.Snowflake}> changed to `{current.Position}` from `{previous?.Position}`")
                .WithCurrentTimestamp()
                .WithColor(Color.Blue));
        }
		*/

        bool previousNsfw = false;
        bool currentNsfw = false;
        string? previousTopic = null;
        string? currentTopic = null;
        ulong? previousCategory = null;
        ulong? currentCategory = null;
        var currentOverwrite = Array.Empty<CacheOverwrite>();
        var previousOverwrite = Array.Empty<CacheOverwrite>();
        
        if (current is CacheTextChannelModel currentText)
        {
            currentNsfw = currentText.IsNsfw;
            currentTopic = currentText.Topic;
            currentCategory = currentText.CategoryId;
            currentOverwrite = currentText.PermissionOverwrites;
        }
        if (previous is CacheTextChannelModel previousText)
        {
            previousNsfw = previousText.IsNsfw;
            previousTopic = previousText.Topic;
            previousCategory = previousText.CategoryId;
            previousOverwrite = previousText.PermissionOverwrites;
        }
        
        
        if (currentNsfw != previousNsfw)
        {
            await _serverLog.EventHandle(current.Guild.Snowflake, (v) => v.ChannelEditChannel, new EmbedBuilder()
                .WithTitle("Channel NSFW State changed")
                .WithDescription($"<#{current.Snowflake}> set to `{currentNsfw}` from `{previousNsfw}`")
                .WithCurrentTimestamp()
                .WithColor(Color.Blue));
        }

        if (currentTopic != previousTopic)
        {
            previousTopic ??= "";
            previousTopic = previousTopic.Length > 0 ? $"`{previousTopic}`" : "<empty>";
            
            currentTopic ??= "";
            currentTopic = currentTopic.Length > 0 ? $"`{currentTopic}`" : "<empty>";
            
            await _serverLog.EventHandle(current.Guild.Snowflake, (v) => v.ChannelEditChannel, new EmbedBuilder()
                .WithTitle("Channel Topic changed")
                .WithDescription($"<#{current.Snowflake}>")
                .AddField("Previous", previousTopic)
                .AddField("Current", currentTopic)
                .WithCurrentTimestamp()
                .WithColor(Color.Blue));
        }

        if (currentCategory != previousCategory)
        {
            var pc = previousCategory == null ? "<null>" : $"<#{previousCategory}>";
            await _serverLog.EventHandle(current.Guild.Snowflake, (v) => v.ChannelEditChannel, new EmbedBuilder()
                .WithTitle("Channel Category changed")
                .WithDescription($"<#{current.Snowflake}>. Moved to <#{currentCategory}> from {pc}")
                .WithCurrentTimestamp()
                .WithColor(Color.Blue));
        }

        int currentBitrate = 0;
        int previousBitrate = 0;
        int? currentUserLimit = null;
        int? previousUserLimit = null;
        VideoQualityMode currentQuality = VideoQualityMode.Auto;
        VideoQualityMode previousQuality = VideoQualityMode.Auto;
        string currentRegion = "";
        string previousRegion = "";

        if (current is CacheVoiceChannelModel currentVoice)
        {
            currentBitrate = currentVoice.Bitrate;
            currentUserLimit = currentVoice.UserLimit;
            currentQuality = currentVoice.VideoQualityMode;
            currentRegion = currentVoice.RTCRegion;
        }
        if (previous is CacheVoiceChannelModel prevVoice)
        {
            previousBitrate = prevVoice.Bitrate;
            previousUserLimit = prevVoice.UserLimit;
            previousQuality = prevVoice.VideoQualityMode;
            previousRegion = prevVoice.RTCRegion;
        }

        if (currentBitrate != previousBitrate)
        {
            await _serverLog.EventHandle(current.Guild.Snowflake, (v) => v.ChannelEditChannel, new EmbedBuilder()
                .WithTitle("Channel Bitrate changed")
                .WithDescription($"<#{current.Snowflake}>. to `{currentBitrate}` from `{previousBitrate}`")
                .WithCurrentTimestamp()
                .WithColor(Color.Blue));
        }

        if (currentUserLimit != previousUserLimit)
        {
            await _serverLog.EventHandle(current.Guild.Snowflake, (v) => v.ChannelEditChannel, new EmbedBuilder()
                .WithTitle("Channel User Limit changed")
                .WithDescription($"<#{current.Snowflake}>. to `{currentUserLimit}` from `{previousUserLimit}`")
                .WithCurrentTimestamp()
                .WithColor(Color.Blue));
        }

        if (currentQuality != previousQuality)
        {
            await _serverLog.EventHandle(current.Guild.Snowflake, (v) => v.ChannelEditChannel, new EmbedBuilder()
                .WithTitle("Channel Video Quality changed")
                .WithDescription($"<#{current.Snowflake}>. to `{currentQuality}` from `{previousQuality}`")
                .WithCurrentTimestamp()
                .WithColor(Color.Blue));
        }

        if (currentRegion != previousRegion)
        {
            await _serverLog.EventHandle(current.Guild.Snowflake, (v) => v.ChannelEditChannel, new EmbedBuilder()
                .WithTitle("Channel Region changed")
                .WithDescription($"<#{current.Snowflake}>. to `{currentRegion}` from `{previousRegion}`")
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
        // Ignore non-guild users
        if (!(user is SocketGuildUser guildUser))
            return;
        
        var changeList = new List<string>();
        if (previous.IsStreaming != current.IsStreaming)
            changeList.Add(current.IsStreaming ? "+ Started Streaming" : "- Stopped Streaming");
        if (previous.IsVideoing != current.IsVideoing)
            changeList.Add(current.IsVideoing ? "+ Camera Enabled" : "- Camera Disabled");
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
        embed.WithDescription($"<@{user.Id}>");
        if (currentChannel != null && previousChannel == null)
            embed.Description += $" Joined <#{currentChannel.Id}>";
        else if (currentChannel == null && previousChannel != null)
            embed.Description += $" Left <#{previous.VoiceChannel.Id}>";
        else if (currentChannel != null && previousChannel != null)
            if (currentChannel.Id != previousChannel.Id)
                embed.Description += $" Switched from <#{previousChannel.Id}> to <#{currentChannel.Id}>";
        
        await _serverLog.EventHandle(guildUser.Guild.Id, (v) => v.MemberVoiceChangeChannel, embed);
    }
}