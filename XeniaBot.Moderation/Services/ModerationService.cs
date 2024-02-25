using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Data.Moderation;
using XeniaBot.Data.Moderation.Models;
using XeniaBot.Data.Moderation.Repositories;
using XeniaBot.Shared;

namespace XeniaBot.Moderation.Services;

[XeniaController]
public partial class ModerationService : BaseService
{
    private readonly BanHistoryRepository _banHistoryRepo;
    private readonly BanRecordRepository _banRecordRepo;
    private readonly KickRecordRepository _kickRecordRepo;
    private readonly AuditLogCheckRepository _auditCheckRepo;
    private readonly DiscordSocketClient _discordClient;
    public ModerationService(IServiceProvider services)
        : base(services)
    {
        _banHistoryRepo = services.GetRequiredService<BanHistoryRepository>();
        _banRecordRepo = services.GetRequiredService<BanRecordRepository>();
        _kickRecordRepo = services.GetRequiredService<KickRecordRepository>();
        _auditCheckRepo = services.GetRequiredService<AuditLogCheckRepository>();
        _discordClient = services.GetRequiredService<DiscordSocketClient>();
    }

    public override Task OnReady()
    {
        _discordClient.UserBanned += DiscordClientUserBanned;
        _discordClient.UserUnbanned += DiscordClientUserUnbanned;

        InitKickEvent();
        
        return Task.CompletedTask;
    }
}