using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Common.Services;
using XeniaDiscord.Common.Interfaces;
using XeniaDiscord.Common.Services;

namespace XeniaDiscord.Common;

public static class XeniaDiscordCommon
{
    public static void RegisterServices(IServiceCollection services)
    {
        // services
        services.AddSingleton<IBackpackTFService, BackpackTFService>()
            .AddScoped<IConfessionService, ConfessionService>()
            .AddScoped<IBanSyncService, BanSyncService>()
            .AddScoped<ITicketService, TicketService>()
            .AddSingleton<IWeatherApiService, WeatherApiService>();
        //.AddScoped<IWeatherService, WeatherService>()
        //.AddScoped<IConfessionService, ConfessionService>()
        //.AddScoped<IBanSyncService, BanSyncService>()
        //.AddScoped<IDistrowatchService, DistrowatchService>()
        //.AddSingleton<IDistrowatchService, DistrowatchService>()
        //.AddScoped<IWarnService, WarnService>()
        //.AddScoped<IWarnAdminService, WarnAdminService>()
        //
        //.AddScoped<IEmojiService, EmojiService>()
        //.AddSingleton<IEmojiService, EmojiService>();

        // repositories
        //services
        //    .AddScoped<IWarnStrikeRepository, WarnStrikeRepository>()
        //    .AddScoped<IWarnRepository, WarnRepository>();
    }
}
