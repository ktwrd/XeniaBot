using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.Shared.Services;

public class DiscordClientProxy : IDiscordClientProxy
{
    private readonly DiscordShardedClient _client;

    public DiscordClientProxy(IServiceProvider services)
    {
        _client = services.GetRequiredService<DiscordShardedClient>();
    }

    public SocketUser? GetUser(ulong id)
        => ExceptionHelper.RetryOnTimedOut(() => _client.GetUser(id));
    public SocketUser? GetUser(string username, string? discriminator = null)
        => ExceptionHelper.RetryOnTimedOut(() => _client.GetUser(username, discriminator: discriminator));

    public IReadOnlyCollection<SocketGuild> Guilds => _client.Guilds;
    public IReadOnlyCollection<ISocketPrivateChannel> PrivateChannels => _client.PrivateChannels;
    public IReadOnlyCollection<SocketDMChannel> DMChannels => _client.Shards.First().DMChannels;
    public IReadOnlyCollection<SocketGroupChannel> GroupChannels => _client.Shards.First().GroupChannels;
    public BaseSocketClient SocketClient => _client;
}

public interface IDiscordClientProxy
{
    SocketUser? GetUser(ulong id);
    SocketUser? GetUser(string username, string? discriminator = null);

    IReadOnlyCollection<SocketGuild> Guilds { get; }
    IReadOnlyCollection<ISocketPrivateChannel> PrivateChannels { get; }
    IReadOnlyCollection<SocketDMChannel> DMChannels { get; }
    IReadOnlyCollection<SocketGroupChannel> GroupChannels { get; }
    BaseSocketClient SocketClient { get; }
}