using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaDiscord.Common.Handlers;
using XeniaDiscord.Common.Mappers;
using XeniaDiscord.Common.Mappers.DiscordCache;
using XeniaDiscord.Common.Mappers.DiscordSnapshot;
using XeniaDiscord.Common.Services;
using XeniaDiscord.Common.Services.BanSync;
// ReSharper disable CheckNamespace
#pragma warning disable IDE0130
#pragma warning disable S1186

namespace XeniaDiscord;

public static class XeniaDiscordCommon
{
    public static void RegisterServices(
        IServiceCollection services,
        bool includeAsSingleton)
    {
        services.AddSingleton<ApplicationEmoteService>()
            .AddSingleton<BanSyncService>()
            .AddSingleton<DiscordAuditLogService>()
            .AddSingleton<DiscordBotListService>()
            .AddSingleton<DiscordStatisticsService>()
            .AddSingleton<DiscordCacheEventHandler>()
            .AddSingleton<ServerLogEventHandler>()
            .AddSingleton<ServerLogService>()
            .AddSingleton<ValidationService>()
            .AddSingleton<IXeniaOnReady, ApplicationEmoteService>(svc => svc.GetRequiredService<ApplicationEmoteService>());

        RegisterMappers(services);

        services.AddSingleton<DiscordCacheService>()
            .AddSingleton<DiscordSnapshotService>()
            .AddSingleton<UserCacheService>()
            .AddSingleton<GuildApprovalService>()
            .AddSingleton<GuildCacheService>()
            .AddSingleton<RolePreserveService>()
            .AddSingleton<RolePreserveLogService>();
    }

    private static void RegisterMappers(IServiceCollection services)
    {
        DiscordUserToUserCacheModelMapper.RegisterService(services);
        DiscordGuildToGuildCacheModelMapper.RegisterService(services);
        DiscordUserToGuildMemberCacheModelMapper.RegisterService(services);
        RoleSnapshotToCacheModelMapper.RegisterService(services);

        MessageSourceToDtoMapper.RegisterService(services);
        MessageTypeToDtoMapper.RegisterService(services);

        RoleToSnapshotModelMapper.RegisterService(services);
        GuildUserToSnapshotModelMapper.RegisterService(services);
        MessageToSnapshotModelMapper.RegisterService(services);
    }
}
