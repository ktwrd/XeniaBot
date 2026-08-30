using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using XeniaDiscord.Data.Repositories;

namespace XeniaDiscord.Interactions.Modules;

[Group("dev", "Developer commands")]
[DeveloperModule]
[CommandContextType(InteractionContextType.Guild)]
[RequireDeveloper]
[UsedImplicitly]
public partial class DeveloperModule : InteractionModuleBase
{
    private readonly DiscordShardedClient _client;
    private readonly ErrorReportService _error;
    private readonly BanSyncGuildRepository _bansyncGuildRepo;
    private readonly ConfigData _config;
    public DeveloperModule(IServiceProvider services)
    {
        _client = services.GetRequiredService<DiscordShardedClient>();
        _error = services.GetRequiredService<ErrorReportService>();
        _bansyncGuildRepo = services.GetRequiredService<BanSyncGuildRepository>();
        _config = services.GetRequiredService<ConfigData>();
    }
}
