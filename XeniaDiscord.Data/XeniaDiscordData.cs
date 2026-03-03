using Microsoft.Extensions.DependencyInjection;
using XeniaDiscord.Data.Services;

namespace XeniaDiscord;

public static class XeniaDiscordData
{
    public static void RegisterServices(IServiceCollection services)
    {
        services
            .AddSingleton<DatabaseMigrationService>();
    }
}
