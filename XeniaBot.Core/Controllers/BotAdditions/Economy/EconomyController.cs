using System;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;

namespace XeniaBot.Core.Controllers.BotAdditions;

[BotController]
public class EconomyController : BaseController
{
    private readonly DiscordSocketClient _client;
    private readonly DiscordController _discord;
    public EconomyController(IServiceProvider services)
        : base(services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        _discord = services.GetRequiredService<DiscordController>();
    }
}