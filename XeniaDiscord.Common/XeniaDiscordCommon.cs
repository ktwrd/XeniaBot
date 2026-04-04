using Microsoft.Extensions.DependencyInjection;
using XeniaDiscord.Common.Handlers;
using XeniaDiscord.Common.Mappers.DiscordCache;
using XeniaDiscord.Common.Mappers.DiscordSnapshot;
using XeniaDiscord.Common.Services;

namespace XeniaDiscord;

public static class XeniaDiscordCommon
{
    public static void RegisterServices(
        IServiceCollection services,
        bool includeAsSingleton)
    {
        services.AddSingleton<BanSyncService>()
                .AddSingleton<ValidationService>()
                .AddSingleton<DiscordStatisticsService>()
                .AddSingleton<DiscordCacheEventHandler>();

        RegisterMappers(services);

        var types = new[]
        {
            typeof(DiscordCacheService),
            typeof(DiscordSnapshotService),

            typeof(UserCacheService),
            typeof(GuildCacheService),

            typeof(GuildApprovalService),
        };
        foreach (var t in types)
        {
            if (includeAsSingleton)
            {
                services.AddSingleton(t);
            }
            else
            {
                services.AddScoped(t);
            }
        }
    }
    private static void RegisterMappers(IServiceCollection services)
    {
        DiscordUserToUserCacheModelMapper.RegisterService(services);
        DiscordGuildToGuildCacheModelMapper.RegisterService(services);
        DiscordUserToGuildMemberCacheModelMapper.RegisterService(services);

        RoleToSnapshotModelMapper.RegisterService(services);
        GuildUserToSnapshotModelMapper.RegisterService(services);
    }
}
