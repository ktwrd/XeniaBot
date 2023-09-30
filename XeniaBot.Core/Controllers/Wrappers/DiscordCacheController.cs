using System;
using System.Diagnostics;
using System.Threading.Tasks;
using XeniaBot.DiscordCache.Helpers;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using XeniaBot.Data.Controllers;
using XeniaBot.Data.Models.Archival;
using XeniaBot.DiscordCache.Controllers;
using XeniaBot.DiscordCache.Models;
using XeniaBot.Shared;

namespace XeniaBot.Core.Controllers.Wrappers;

[BotController]
public class DiscordCacheController : BaseController
{

    public DiscordCacheGenericConfigController<CacheMessageModel> CacheMessageConfig;
    public DiscordCacheGenericConfigController<CacheUserModel> CacheUserConfig;
    public DiscordCacheGenericConfigController<CacheGuildMemberModel> CacheGuildMemberConfig;
    public DiscordCacheGenericConfigController<CacheGuildModel> CacheGuildConfig;

    public DiscordCacheGenericConfigController<CacheForumChannelModel> CacheForumChannelConfig;
    public DiscordCacheGenericConfigController<CacheVoiceChannelModel> CacheVoiceChannelConfig;
    public DiscordCacheGenericConfigController<CacheStageChannelModel> CacheStageChannelConfig;
    public DiscordCacheGenericConfigController<CacheTextChannelModel> CacheTextChannelConfig;
    private readonly UserConfigController _userConfig;
    private readonly DiscordSocketClient _client;
    public DiscordCacheController(IServiceProvider services)
        : base(services)
    {
        _userConfig = services.GetRequiredService<UserConfigController>();
        _client = services.GetRequiredService<DiscordSocketClient>();
        CacheMessageConfig = new DiscordCacheGenericConfigController<CacheMessageModel>("bb_store_message", services);
        CacheUserConfig = new DiscordCacheGenericConfigController<CacheUserModel>("cache_store_user", services);
        CacheGuildMemberConfig =
            new DiscordCacheGenericConfigController<CacheGuildMemberModel>("cache_store_guild_member", services);
        CacheGuildConfig = new DiscordCacheGenericConfigController<CacheGuildModel>("cache_store_guild", services);
        
        
        CacheForumChannelConfig =
            new DiscordCacheGenericConfigController<CacheForumChannelModel>("cache_store_channel_forum", services);
        CacheVoiceChannelConfig =
            new DiscordCacheGenericConfigController<CacheVoiceChannelModel>("cache_store_channel_voice", services);
        CacheStageChannelConfig =
            new DiscordCacheGenericConfigController<CacheStageChannelModel>("cache_store_channel_stage", services);
        CacheTextChannelConfig =
            new DiscordCacheGenericConfigController<CacheTextChannelModel>("cache_store_channel_text", services);
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
        var userConfig = await _userConfig.GetOrDefault(previous.Id);
        if (!userConfig.EnableProfileTracking)
            return;
        var data = await CacheUserConfig.GetLatest(previous.Id);
        var currentData = CacheUserModel.FromUser(current);
        await CacheUserConfig.Add(currentData);
        OnUserChange(
            CacheChangeType.Update,
            currentData,
            data);
    }

    private async Task _client_GuildUpdated(SocketGuild previous, SocketGuild current)
    {
        var data = await CacheGuildConfig.GetLatest(previous.Id);
        var currentData = CacheGuildModel.FromGuild(current);
        await CacheGuildConfig.Add(currentData);
        OnGuildChange(
            CacheChangeType.Update,
            currentData,
            data);
    }

    private async Task _client_GuildJoined(SocketGuild current)
    {
        var data = await CacheGuildConfig.GetLatest(current.Id);
        var currentData = CacheGuildModel.FromGuild(current);
        await CacheGuildConfig.Add(currentData);
        OnGuildChange(
            CacheChangeType.Create,
            currentData,
            data);
    }
    
    /// <summary>
    /// Invoked when <see cref="DiscordSocketClient.GuildMemberUpdated"/> is fired.
    /// </summary>
    /// <param name="oldMember">Previous member state</param>
    /// <param name="newMember">Current member state</param>
    private async Task _client_GuildMemberUpdated(Cacheable<SocketGuildUser, ulong> oldMember,
        SocketGuildUser newMember)
    {
        var data = await CacheGuildMemberConfig.GetLatest(oldMember.Id);
        var currentData = CacheGuildMemberModel.FromGuildMember(newMember);
        await CacheGuildMemberConfig.Add(currentData);
        OnGuildMemberChange(
            CacheChangeType.Update,
            currentData, 
            data);
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
                    var forumData = await CacheForumChannelConfig.GetLatest(forumChannel.Id);
                    var forumCurrentData = new CacheForumChannelModel().FromExisting(forumChannel);
                    await CacheForumChannelConfig.Add(forumCurrentData);
                    OnChannelChange(CacheChangeType.Update, forumCurrentData, forumData);
                }
                break;
            case CacheChannelType.Voice:
                if (newChannel is SocketVoiceChannel voiceChannel)
                {
                    var voiceData = await CacheVoiceChannelConfig.GetLatest(voiceChannel.Id);
                    var voiceCurrentData = new CacheVoiceChannelModel().FromExisting(voiceChannel);
                    await CacheVoiceChannelConfig.Add(voiceCurrentData);
                    OnChannelChange(CacheChangeType.Update, voiceCurrentData, voiceData);
                }
                break;
            case CacheChannelType.Text:
                if (newChannel is SocketTextChannel textChannel)
                {
                    var textData = await CacheTextChannelConfig.GetLatest(textChannel.Id);
                    var textCurrentData = new CacheTextChannelModel().FromExisting(textChannel);
                    await CacheTextChannelConfig.Add(textCurrentData);
                    OnChannelChange(CacheChangeType.Update, textCurrentData, textData);
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
                    var forumData = await CacheForumChannelConfig.GetLatest(forumChannel.Id);
                    var forumCurrentData = new CacheForumChannelModel().FromExisting(forumChannel);
                    await CacheForumChannelConfig.Add(forumCurrentData);
                    OnChannelChange(CacheChangeType.Create, forumCurrentData, forumData);
                }
                break;
            case CacheChannelType.Voice:
                if (guildChannel is SocketVoiceChannel voiceChannel)
                {
                    var voiceData = await CacheVoiceChannelConfig.GetLatest(voiceChannel.Id);
                    var voiceCurrentData = new CacheVoiceChannelModel().FromExisting(voiceChannel);
                    await CacheVoiceChannelConfig.Add(voiceCurrentData);
                    OnChannelChange(CacheChangeType.Create, voiceCurrentData, voiceData);
                }
                break;
            case CacheChannelType.Text:
                if (guildChannel is SocketTextChannel textChannel)
                {
                    var textData = await CacheTextChannelConfig.GetLatest(textChannel.Id);
                    var textCurrentData = new CacheTextChannelModel().FromExisting(textChannel);
                    await CacheTextChannelConfig.Add(textCurrentData);
                    OnChannelChange(CacheChangeType.Create, textCurrentData, textData);
                }
                break;
        }
    }
    
    #region Message
    private async Task _client_MessageReceived(SocketMessage message)
    {
        var data = CacheMessageModel.FromMessage(message);
        if (message.Channel is SocketGuildChannel socketChannel)
            data.GuildId = socketChannel.Id;
        await CacheMessageConfig.Add(data);
        OnMessageChange(
            MessageChangeType.Create, 
            data, 
            null);
    }

    private async Task _client_MessageUpdated(
        Cacheable<IMessage, ulong> useless_object,
        SocketMessage newMessage,
        ISocketMessageChannel channel)
    {
        // convert data to type that mongo can support
        var data = CacheMessageModel.FromMessage(newMessage);
        
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

    private async Task _client_MessageDeleted(Cacheable<IMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel)
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
    #endregion
}
