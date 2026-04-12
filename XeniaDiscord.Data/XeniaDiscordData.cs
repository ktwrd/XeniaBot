using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaDiscord.Data.Repositories;
using XeniaDiscord.Data.Services;

namespace XeniaDiscord;

public static class XeniaDiscordData
{
    public static void RegisterServices(
        IServiceCollection services,
        bool includeAsSingleton)
    {
        if (includeAsSingleton)
        {
            services.AddSingleton<DatabaseMigrationService>();
        }
        else
        {
            services.AddScoped<DatabaseMigrationService>();
        }

        RegisterRepositories(services, includeAsSingleton);
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

            typeof(ServerLogRepository),
            typeof(GuildApprovalRepository),
            typeof(RolePreserveGuildRepository),
            typeof(RolePreserveUserRepository),
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
