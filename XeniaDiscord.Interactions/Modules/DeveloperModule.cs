using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Frozen;
using System.Text;
using System.Text.Json;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Repositories;

namespace XeniaDiscord.Interactions.Modules;

[Group("dev", "Developer commands")]
[DeveloperModule]
[CommandContextType(InteractionContextType.Guild)]
public partial class DeveloperModule : InteractionModuleBase
{
    private readonly DiscordSocketClient _client;
    private readonly ErrorReportService _error;
    private readonly BanSyncGuildRepository _bansyncGuildRepo;
    public DeveloperModule(IServiceProvider services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        _error = services.GetRequiredService<ErrorReportService>();
        _bansyncGuildRepo = services.GetRequiredService<BanSyncGuildRepository>();
    }
}
