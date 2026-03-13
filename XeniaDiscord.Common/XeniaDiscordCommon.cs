using Microsoft.Extensions.DependencyInjection;
using XeniaDiscord.Common.Handlers;
using XeniaDiscord.Common.Services;

namespace XeniaDiscord;

public static class XeniaDiscordCommon
{
    public static void RegisterServices(
        IServiceCollection services,
        bool includeAsSingleton)
    {
        services.AddSingleton<BanSyncService>()
            .AddSingleton<DiscordCacheEventHandler>()
            .AddSingleton<UserCacheService>();

        services.AddScoped<DiscordCacheService>();
        if (!includeAsSingleton) return;
        services.AddSingleton<DiscordCacheService>();
    }
}
