using Microsoft.Extensions.DependencyInjection;
using XeniaDiscord.Data.Repositories;
using XeniaDiscord.Data.Services;

namespace XeniaDiscord;

public static class XeniaDiscordData
{
    public static void RegisterServices(
        IServiceCollection services,
        bool includeAsSingleton)
    {
        services.AddScoped<DatabaseMigrationService>();

        RegisterRepositories(services, includeAsSingleton);

        if (!includeAsSingleton) return;

        services.AddSingleton<DatabaseMigrationService>();
    }
    public static void RegisterRepositories(
        IServiceCollection services,
        bool includeAsSingleton = false)
    {
        var types = new[]
        {
            typeof(AuditLogEntryCacheRepository),

            typeof(BanSyncGuildRepository),
            typeof(BanSyncRecordRepository),
            typeof(BanSyncGuildSnapshotRepository),
            
            typeof(GuildCacheRepository),
            typeof(GuildMemberCacheRepository),
            typeof(UserCacheRepository),

            typeof(ServerLogRepository)
        };
        foreach (var i in types)
        {
            if (includeAsSingleton)
            {
                services.AddSingleton(i);
            }
            else
            {
                services.AddScoped(i);
            }
        }
    }
}
