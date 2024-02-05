using System;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaBot.Shared.Controllers;

namespace XeniaBot.Core.Services.BotAdditions;

[XeniaController]
public class EconomyService : BaseController
{
    private readonly DiscordSocketClient _client;
    private readonly DiscordController _discord;
    public EconomyService(IServiceProvider services)
        : base(services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        _discord = services.GetRequiredService<DiscordController>();
    }
}