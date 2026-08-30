using System;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;

namespace XeniaBot.Core.Services.BotAdditions;

[XeniaController]
public class EconomyService : BaseService
{
    private readonly DiscordShardedClient _client;
    private readonly DiscordService _discord;
    public EconomyService(IServiceProvider services)
        : base(services)
    {
        _client = services.GetRequiredService<DiscordShardedClient>();
        _discord = services.GetRequiredService<DiscordService>();
    }
}