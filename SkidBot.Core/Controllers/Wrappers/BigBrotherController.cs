using System;
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
    private readonly DiscordSocketClient _client;
    public BigBrotherController(IServiceProvider services)
        : base(services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        BBMessageConfig = new BigBrotherGenericConfigController<BB_MessageModel>("bb_store_message", services);

        _client.MessageReceived += _client_MessageReceived;
        _client.MessageUpdated += _client_MessageUpdated;
        _client.MessageDeleted += _client_MessageDeleted;
    }

    private async Task _client_MessageReceived(SocketMessage message)
    {
        var data = BB_MessageModel.FromMessage(message);
        if (message.Channel is SocketGuildChannel socketChannel)
            data.GuildId = socketChannel.Id;
        await BBMessageConfig.Set(data);
    }

    private async Task _client_MessageUpdated(Cacheable<IMessage, ulong> oldMessage, SocketMessage newMessage, ISocketMessageChannel channel)
    {
        var data = BB_MessageModel.FromMessage(newMessage);
        if (channel is SocketGuildChannel { Guild: not null } socketChannel)
            data.GuildId = socketChannel.Guild.Id;
        await BBMessageConfig.Set(data);
    }

    private async Task _client_MessageDeleted(Cacheable<IMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel)
    {
        var data = await BBMessageConfig.Get(message.Id);
        if (data != null)
        {
            data.IsDeleted = true;
            data.DeletedTimestamp = DateTimeOffset.UtcNow;
            await BBMessageConfig.Set(data);
        }
    }
}
