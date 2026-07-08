using Microsoft.Extensions.DependencyInjection;
using XeniaDiscord.Data.Repositories;
using XeniaDiscord.Data.Services;
// ReSharper disable CheckNamespace
#pragma warning disable IDE0130
#pragma warning disable S1186

namespace XeniaDiscord;

public static class XeniaDiscordData
{
    public static void RegisterServices(
        IServiceCollection services,
        bool includeAsSingleton)
    {
        services.AddSingleton<DatabaseMigrationService>();
        RegisterRepositories(services);
    }

    public static void RegisterRepositories(
        IServiceCollection services)
    {
        services.AddSingleton<AuditLogEntryCacheRepository>()
            .AddSingleton<BanSyncGuildRepository>()
            .AddSingleton<BanSyncRecordRepository>()
            .AddSingleton<BanSyncGuildSnapshotRepository>()
            .AddSingleton<GuildApprovalRepository>()
            .AddSingleton<GuildCacheRepository>()
            .AddSingleton<GuildMemberCacheRepository>()
            .AddSingleton<UserCacheRepository>()
            .AddSingleton<ServerLogRepository>()
            .AddSingleton<RolePreserveGuildRepository>()
            .AddSingleton<RolePreserveUserRepository>();
    }
}
