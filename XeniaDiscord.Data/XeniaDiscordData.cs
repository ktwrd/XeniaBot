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
        services.AddScoped<BanSyncGuildRepository>();
        if (!includeAsSingleton) return;
        services.AddSingleton<BanSyncGuildRepository>();
    }
}
