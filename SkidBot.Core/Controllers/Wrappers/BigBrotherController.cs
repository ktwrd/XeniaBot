using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using SkidBot.Core.Controllers.Wrappers.BigBrother;
using SkidBot.Shared;

namespace SkidBot.Core.Controllers.Wrappers;

[SkidController]
public class BigBrotherController : BaseController
{

    public BigBrotherGenericConfigController<BB_MessageModel> BBMessageConfig;
    public BigBrotherGenericConfigController<BB_UserModel> BBUserConfig;
    public BigBrotherGenericConfigController<BB_ChannelModel> BBChannelConfig;
    private readonly DiscordSocketClient _client;
    public BigBrotherController(IServiceProvider services)
        : base(services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        BBMessageConfig = new BigBrotherGenericConfigController<BB_MessageModel>("bb_store_message", services);
    }

    public override Task InitializeAsync()
    {
        _client.MessageReceived += _client_MessageReceived;
        _client.MessageUpdated += _client_MessageUpdated;
        _client.MessageDeleted += _client_MessageDeleted;
        _client.UserUpdated += _client_UserUpdated;
        _client.ChannelUpdated += _client_ChannelUpdated;
        return Task.CompletedTask;
    }

    public event MessageDiffDelegate MessageChange;
    public event UserDiffDelegate UserChange;
    private void OnMessageChange(MessageChangeType type, BB_MessageModel current, BB_MessageModel? previous)
    {
        if (MessageChange != null)
        {
            MessageChange?.Invoke(type, current, previous);
        }
    }

    private void OnUserChange(UserChangeType type, BB_UserModel current, BB_UserModel? previous)
    {
        
    }

    private async Task _client_UserUpdated(SocketUser previous, SocketUser current)
    {
        var data = await BBUserConfig.Get(previous.Id);
        var currentData = BB_UserModel.FromUser(current);
        await BBUserConfig.Set(currentData);
        OnUserChange(
            UserChangeType.Update,
            currentData,
            data);
    }

    private async Task _client_ChannelUpdated(SocketChannel oldChannel, SocketChannel newChannel)
    {
        
    }
    
    #region Message
    private async Task _client_MessageReceived(SocketMessage message)
    {
        var data = BB_MessageModel.FromMessage(message);
        if (message.Channel is SocketGuildChannel socketChannel)
            data.GuildId = socketChannel.Id;
        await BBMessageConfig.Set(data);
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
        var data = BB_MessageModel.FromMessage(newMessage);
        
        // set guild if message was actually sent in a server (and not dms)
        if (channel is SocketGuildChannel { Guild: not null } socketChannel)
            data.GuildId = socketChannel.Guild.Id;
        // fetch previous message for event emit
        var previous = await BBMessageConfig.Get(data.Snowflake);
        
        // save in db
        await BBMessageConfig.Set(data);
        
        // emit event for other controllers.
        OnMessageChange(
            MessageChangeType.Update,
            data,
            previous);
    }

    private async Task _client_MessageDeleted(Cacheable<IMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel)
    {
        var data = await BBMessageConfig.Get(message.Id);
        if (data != null)
        {
            var previous = data.Clone();
            data.IsDeleted = true;
            data.DeletedTimestamp = DateTimeOffset.UtcNow;
            await BBMessageConfig.Set(data);
            OnMessageChange(
                MessageChangeType.Delete,
                data,
                previous);
        }
    }
    #endregion
}
