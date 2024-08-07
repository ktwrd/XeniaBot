﻿using System;
using System.Data;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using XeniaBot.DiscordCache.Helpers;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using XeniaBot.Core.Helpers;
using XeniaBot.Data.Models.Archival;
using XeniaBot.Data.Repositories;
using XeniaBot.DiscordCache.Controllers;
using XeniaBot.DiscordCache.Models;
using XeniaBot.Shared;

namespace XeniaBot.Core.Services.Wrappers;

[XeniaController]
public class DiscordCacheService : BaseService
{

    public DiscordCacheGenericRepository<CacheMessageModel> CacheMessageConfig;
    public DiscordCacheGenericRepository<CacheUserModel> CacheUserConfig;
    public DiscordCacheGenericRepository<CacheGuildMemberModel> CacheGuildMemberConfig;
    public DiscordCacheGenericRepository<CacheGuildModel> CacheGuildConfig;

    public DiscordCacheGenericRepository<CacheForumChannelModel> CacheForumChannelConfig;
    public DiscordCacheGenericRepository<CacheVoiceChannelModel> CacheVoiceChannelConfig;
    public DiscordCacheGenericRepository<CacheStageChannelModel> CacheStageChannelConfig;
    public DiscordCacheGenericRepository<CacheTextChannelModel> CacheTextChannelConfig;
    private readonly UserConfigRepository _userConfig;
    private readonly DiscordSocketClient _client;
    public DiscordCacheService(IServiceProvider services)
        : base(services)
    {
        _userConfig = services.GetRequiredService<UserConfigRepository>();
        _client = services.GetRequiredService<DiscordSocketClient>();
        CacheMessageConfig = new DiscordCacheGenericRepository<CacheMessageModel>(CacheMessageModel.CollectionName, services);
        CacheUserConfig = new DiscordCacheGenericRepository<CacheUserModel>(CacheUserModel.CollectionName, services);
        CacheGuildMemberConfig =
            new DiscordCacheGenericRepository<CacheGuildMemberModel>(CacheGuildMemberModel.CollectionName, services);
        CacheGuildConfig = new DiscordCacheGenericRepository<CacheGuildModel>(CacheGuildModel.CollectionName, services);
        
        
        CacheForumChannelConfig =
            new DiscordCacheGenericRepository<CacheForumChannelModel>(CacheForumChannelModel.CollectionName, services);
        CacheVoiceChannelConfig =
            new DiscordCacheGenericRepository<CacheVoiceChannelModel>(CacheVoiceChannelModel.CollectionName, services);
        CacheStageChannelConfig =
            new DiscordCacheGenericRepository<CacheStageChannelModel>(CacheStageChannelModel.CollectionName, services);
        CacheTextChannelConfig =
            new DiscordCacheGenericRepository<CacheTextChannelModel>(CacheTextChannelModel.CollectionName, services);
    }

    public override Task InitializeAsync()
    {
        _client.MessageReceived += _client_MessageReceived;
        _client.MessageUpdated += _client_MessageUpdated;
        _client.MessageDeleted += _client_MessageDeleted;
        
        _client.UserUpdated += _client_UserUpdated;

        _client.GuildUpdated += _client_GuildUpdated;
        _client.JoinedGuild += _client_GuildJoined;
        _client.GuildMemberUpdated += _client_GuildMemberUpdated;

        _client.ChannelUpdated += _client_ChannelUpdated;
        _client.ChannelCreated += _client_ChannelCreated;
        return Task.CompletedTask;
    }

    public event MessageDiffDelegate MessageChange;
    public event UserDiffDelegate UserChange;
    public event GuildMemberDiffDelegate GuildMemberChange;
    public event GuildDiffDelegate GuildChange;
    public event ChannelDiffDelegate ChannelChange;
    private void OnMessageChange(MessageChangeType type, CacheMessageModel current, CacheMessageModel? previous)
    {
        if (MessageChange != null)
        {
            MessageChange?.Invoke(type, current, previous);
        }
    }

    private void OnUserChange(CacheChangeType type, CacheUserModel current, CacheUserModel? previous)
    {
        if (UserChange != null)
        {
            UserChange?.Invoke(type, current, previous);
        }
    }

    private void OnGuildMemberChange(CacheChangeType type,
        CacheGuildMemberModel current,
        CacheGuildMemberModel? previous)
    {
        if (GuildMemberChange != null)
        {
            GuildMemberChange?.Invoke(type, current, previous);
        }
    }

    private void OnGuildChange(CacheChangeType type,
        CacheGuildModel current,
        CacheGuildModel? previous)
    {
        if (GuildChange != null)
        {
            GuildChange?.Invoke(type, current, previous);
        }
    }

    private void OnChannelChange(CacheChangeType type,
        CacheGuildChannelModel current,
        CacheGuildChannelModel? previous)
    {
        if (ChannelChange != null)
            ChannelChange?.Invoke(type, current, previous);
    }

    private async Task _client_UserUpdated(SocketUser previous, SocketUser current)
    {
        try
        {
            var userConfig = await _userConfig.GetOrDefault(previous.Id);
            if (!userConfig.EnableProfileTracking)
                return;
            var data = await CacheUserConfig.GetLatest(previous.Id);
            var currentData = CacheUserModel.FromExisting(current);
            if (currentData == null)
                throw new NoNullAllowedException("currentData is null");
            await CacheUserConfig.Add(currentData);
            OnUserChange(
                CacheChangeType.Update,
                currentData,
                data);
        }
        catch (Exception ex)
        {
            var oldJson = JsonSerializer.Serialize(previous, Program.SerializerOptions);
            var newJson = JsonSerializer.Serialize(current, Program.SerializerOptions);
            await DiscordHelper.ReportError(ex, string.Join("\n", new string[]
            {
                $"Failed to run DiscordCacheService._client_GuildUpdated ({current.Id})\n",
                "Old JSON:",
                "```json",
                oldJson,
                "```",
                "New JSON:",
                "```json",
                newJson,
                "```"
            }));
        }
    }

    private async Task _client_GuildUpdated(SocketGuild previous, SocketGuild current)
    {
        try
        {
            var data = await CacheGuildConfig.GetLatest(previous.Id);
            var currentData = CacheGuildModel.FromGuild(current);
            await CacheGuildConfig.Add(currentData);
            OnGuildChange(
                CacheChangeType.Update,
                currentData,
                data);
        }
        catch (Exception ex)
        {
            var oldJson = JsonSerializer.Serialize(previous, Program.SerializerOptions);
            var newJson = JsonSerializer.Serialize(current, Program.SerializerOptions);
            await DiscordHelper.ReportError(ex, string.Join("\n", new string[]
            {
                $"Failed to run DiscordCacheService._client_GuildUpdated ({current.Id})\n",
                "Old JSON:",
                "```json",
                oldJson,
                "```",
                "New JSON:",
                "```json",
                newJson,
                "```"
            }));
        }
    }

    private async Task _client_GuildJoined(SocketGuild current)
    {
        try
        {
            var data = await CacheGuildConfig.GetLatest(current.Id);
            var currentData = CacheGuildModel.FromGuild(current);
            await CacheGuildConfig.Add(currentData);
            OnGuildChange(
                CacheChangeType.Create,
                currentData,
                data);
        }
        catch (Exception ex)
        {
            var oldJson = JsonSerializer.Serialize(current, Program.SerializerOptions);
            await DiscordHelper.ReportError(ex, string.Join("\n", new string[]
            {
                $"Failed to run DiscordCacheService._client_GuildJoined ({current.Id})\n",
                "Guild JSON:",
                "```json",
                oldJson,
                "```",
            }));
        }
    }
    
    /// <summary>
    /// Invoked when <see cref="DiscordSocketClient.GuildMemberUpdated"/> is fired.
    /// </summary>
    /// <param name="oldMember">Previous member state</param>
    /// <param name="newMember">Current member state</param>
    private async Task _client_GuildMemberUpdated(Cacheable<SocketGuildUser, ulong> oldMember,
        SocketGuildUser newMember)
    {
        try
        {
            var data = await CacheGuildMemberConfig.GetLatest(oldMember.Id);
            var currentData = CacheGuildMemberModel.FromExisting(newMember);
            if (currentData == null)
                throw new NoNullAllowedException("currentData is null");
            await CacheGuildMemberConfig.Add(currentData);
            OnGuildMemberChange(
                CacheChangeType.Update,
                currentData, 
                data);
        }
        catch (Exception ex)
        {
            var oldJson = JsonSerializer.Serialize(oldMember.Value, Program.SerializerOptions);
            var newJson = JsonSerializer.Serialize(newMember, Program.SerializerOptions);
            await DiscordHelper.ReportError(ex, string.Join("\n", new string[]
            {
                $"Failed to run DiscordCacheService._client_GuildMemberUpdated ({oldMember.Id} in {newMember?.Guild.Id})\n",
                "Old JSON:",
                "```json",
                oldJson,
                "```",
                "New JSON:",
                "```json",
                newJson,
                "```"
            }));
        }
    }

    private async Task _client_ChannelUpdated(SocketChannel oldChannel, SocketChannel newChannel)
    {
        if (!(newChannel is SocketGuildChannel guildChannel))
            return;

        var channelType = DiscordCacheHelper.GetChannelType(guildChannel);
        switch (channelType)
        {
            case CacheChannelType.Forum:
                if (newChannel is SocketForumChannel forumChannel)
                {
                    try
                    {
                        var forumData = await CacheForumChannelConfig.GetLatest(forumChannel.Id);
                        var forumCurrentData = new CacheForumChannelModel().Update(forumChannel);
                        await CacheForumChannelConfig.Add(forumCurrentData);
                        OnChannelChange(CacheChangeType.Update, forumCurrentData, forumData);
                    }
                    catch (Exception ex)
                    {
                        var channelJson = JsonSerializer.Serialize(newChannel, Program.SerializerOptions);
                        await DiscordHelper.ReportError(ex, string.Join("\n", new string[]
                        {
                            $"Failed to process forum channel in DiscordCacheService._client_ChannelUpdated ({guildChannel.Id})",
                            "```json",
                            channelJson,
                            "```"
                        }));
                    }
                }
                break;
            case CacheChannelType.Voice:
                if (newChannel is SocketVoiceChannel voiceChannel)
                {
                    try
                    {
                        var voiceData = await CacheVoiceChannelConfig.GetLatest(voiceChannel.Id);
                        var voiceCurrentData = new CacheVoiceChannelModel().Update(voiceChannel);
                        await CacheVoiceChannelConfig.Add(voiceCurrentData);
                        OnChannelChange(CacheChangeType.Update, voiceCurrentData, voiceData);
                    }
                    catch (Exception ex)
                    {
                        var channelJson = JsonSerializer.Serialize(newChannel, Program.SerializerOptions);
                        await DiscordHelper.ReportError(ex, string.Join("\n", new string[]
                        {
                            $"Failed to process voice channel in DiscordCacheService._client_ChannelUpdated ({guildChannel.Id})",
                            "```json",
                            channelJson,
                            "```"
                        }));
                    }
                }
                break;
            case CacheChannelType.Text:
                if (newChannel is SocketTextChannel textChannel)
                {
                    try
                    {
                        var textData = await CacheTextChannelConfig.GetLatest(textChannel.Id);
                        var textCurrentData = new CacheTextChannelModel().Update(textChannel);
                        await CacheTextChannelConfig.Add(textCurrentData);
                        OnChannelChange(CacheChangeType.Update, textCurrentData, textData);
                    }
                    catch (Exception ex)
                    {
                        var channelJson = JsonSerializer.Serialize(newChannel as ITextChannel, Program.SerializerOptions);
                        await DiscordHelper.ReportError(ex, string.Join("\n", new string[]
                        {
                            $"Failed to process text channel in DiscordCacheService._client_ChannelUpdated ({guildChannel.Id})",
                            "```json",
                            channelJson,
                            "```"
                        }));
                    }
                }
                break;
        }
    }

    private async Task _client_ChannelCreated(SocketChannel channel)
    {
        if (!(channel is SocketGuildChannel guildChannel))
            return;
        
        var channelType = DiscordCacheHelper.GetChannelType(guildChannel);
        switch (channelType)
        {
            case CacheChannelType.Forum:
                if (guildChannel is SocketForumChannel forumChannel)
                {
                    try
                    {
                        var forumData = await CacheForumChannelConfig.GetLatest(forumChannel.Id);
                        var forumCurrentData = new CacheForumChannelModel().Update(forumChannel);
                        await CacheForumChannelConfig.Add(forumCurrentData);
                        OnChannelChange(CacheChangeType.Create, forumCurrentData, forumData);
                    }
                    catch (Exception ex)
                    {
                        var channelJson = JsonSerializer.Serialize(guildChannel, Program.SerializerOptions);
                        await DiscordHelper.ReportError(ex, string.Join("\n", new string[]
                        {
                            $"Failed to process forum channel in DiscordCacheService._client_ChannelCreated ({guildChannel.Id})",
                            "```json",
                            channelJson,
                            "```"
                        }));
                    }
                }
                break;
            case CacheChannelType.Voice:
                if (guildChannel is SocketVoiceChannel voiceChannel)
                {
                    try
                    {
                        var voiceData = await CacheVoiceChannelConfig.GetLatest(voiceChannel.Id);
                        var voiceCurrentData = new CacheVoiceChannelModel().Update(voiceChannel);
                        await CacheVoiceChannelConfig.Add(voiceCurrentData);
                        OnChannelChange(CacheChangeType.Create, voiceCurrentData, voiceData);
                    }
                    catch (Exception ex)
                    {
                        var channelJson = JsonSerializer.Serialize(guildChannel, Program.SerializerOptions);
                        await DiscordHelper.ReportError(ex, string.Join("\n", new string[]
                        {
                            $"Failed to process voice channel in DiscordCacheService._client_ChannelCreated ({guildChannel.Id})",
                            "```json",
                            channelJson,
                            "```"
                        }));
                    }
                }
                break;
            case CacheChannelType.Text:
                if (guildChannel is SocketTextChannel textChannel)
                {
                    try
                    {
                        var textData = await CacheTextChannelConfig.GetLatest(textChannel.Id);
                        var textCurrentData = new CacheTextChannelModel().Update(textChannel);
                        await CacheTextChannelConfig.Add(textCurrentData);
                        OnChannelChange(CacheChangeType.Create, textCurrentData, textData);
                    }
                    catch (Exception ex)
                    {
                        var channelJson = JsonSerializer.Serialize(guildChannel, Program.SerializerOptions);
                        await DiscordHelper.ReportError(ex, string.Join("\n", new string[]
                        {
                            $"Failed to process text channel in DiscordCacheService._client_ChannelCreated ({guildChannel.Id})",
                            "```json",
                            channelJson,
                            "```"
                        }));
                    }
                }
                break;
        }
    }
    
    #region Message
    private async Task _client_MessageReceived(SocketMessage message)
    {
        try
        {
            var data = CacheMessageModel.FromExisting(message);
            if (message.Channel is SocketGuildChannel socketChannel)
                data.GuildId = socketChannel.Id;
            await CacheMessageConfig.Add(data);
            OnMessageChange(
                MessageChangeType.Create, 
                data, 
                null);
        }
        catch (Exception ex)
        {
            var msgJson = JsonSerializer.Serialize(message, new JsonSerializerOptions()
            {
                IncludeFields = true,
                WriteIndented = true,
                ReferenceHandler = ReferenceHandler.Preserve,
            });
            await DiscordHelper.ReportError(ex, string.Join("\n", new string[]
            {
                $"Failed to run DiscordCacheService._client_MessageUpdated ({message.Id} in {message.Channel.Id})\n",
                "Message JSON:",
                "```json",
                msgJson,
                "```"
            }));
        }
    }

    private async Task _client_MessageUpdated(
        Cacheable<IMessage, ulong> previousMessage,
        SocketMessage newMessage,
        ISocketMessageChannel channel)
    {
        try
        {
            // convert data to type that mongo can support
            var data = CacheMessageModel.FromExisting(newMessage);

            // set guild if message was actually sent in a server (and not dms)
            if (channel is SocketGuildChannel { Guild: not null } socketChannel)
                data.GuildId = socketChannel.Guild.Id;
            // fetch previous message for event emit
            var previous = await CacheMessageConfig.GetLatest(data.Snowflake);

            // save in db
            await CacheMessageConfig.Add(data);

            // emit event for other controllers.
            OnMessageChange(
                MessageChangeType.Update,
                data,
                previous);
        }
        catch (Exception ex)
        {
            var msgJson = JsonSerializer.Serialize(newMessage, Program.SerializerOptions);
            var channelJson = JsonSerializer.Serialize(channel, Program.SerializerOptions);
            await DiscordHelper.ReportError(ex, string.Join("\n", new string[]
            {
                $"Failed to run DiscordCacheService._client_MessageUpdated ({previousMessage.Id} in {channel.Id})\n",
                "Message JSON:",
                "```json",
                msgJson,
                "```",
                "Channel JSON:",
                "```json",
                channelJson,
                "```"
            }));
        }
    }

    private async Task _client_MessageDeleted(Cacheable<IMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel)
    {
        try
        {
            var data = await CacheMessageConfig.GetLatest(message.Id);
            if (data != null)
            {
                var previous = data.Clone();
                data.IsDeleted = true;
                data.DeletedTimestamp = DateTimeOffset.UtcNow;
                await CacheMessageConfig.Add(data);
                OnMessageChange(
                    MessageChangeType.Delete,
                    data,
                    previous);
            }
        }
        catch (Exception ex)
        {
            var msgJson = JsonSerializer.Serialize(message.Value, Program.SerializerOptions);
            var channelJson = JsonSerializer.Serialize(channel.Value, Program.SerializerOptions);
            await DiscordHelper.ReportError(ex, string.Join("\n", new string[]
            {
                $"Failed to run DiscordCacheService._client_MessageDeleted ({message.Id} in {channel.Id})\n",
                "Message JSON:",
                "```json",
                msgJson,
                "```",
                "Channel JSON:",
                "```json",
                channelJson,
                "```"
            }));
        }
    }
    #endregion
}
