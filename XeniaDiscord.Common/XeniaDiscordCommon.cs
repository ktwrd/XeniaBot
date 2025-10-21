using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Common.Services;
using XeniaDiscord.Common.Interfaces;
using XeniaDiscord.Common.Repositories;
using XeniaDiscord.Common.Services;

namespace XeniaDiscord.Common;

public static class XeniaDiscordCommon
{
    public static void RegisterServices(IServiceCollection services)
    {
        // services
        services
            .AddScoped<IConfessionService, ConfessionService>()
            .AddScoped<IBanSyncService, BanSyncService>()
            .AddScoped<ITicketService, TicketService>()
            .AddSingleton<IBackpackTFService, BackpackTFService>()
            .AddSingleton<IWeatherApiService, WeatherApiService>()
            .AddSingleton<IWeatherModuleService, WeatherModuleService>()
            .AddSingleton<IGoogleTranslateService, GoogleTranslateService>();
        //.AddScoped<IDistrowatchService, DistrowatchService>()
        //.AddSingleton<IDistrowatchService, DistrowatchService>()
        //.AddScoped<IWarnService, WarnService>()
        //.AddScoped<IWarnAdminService, WarnAdminService>()
        //
        //.AddScoped<IEmojiService, EmojiService>()
        //.AddSingleton<IEmojiService, EmojiService>();

        // repositories
        services
            .AddScoped<IBanSyncGuildRepository, BanSyncGuildRepository>()
            .AddScoped<IBanSyncRepository, BanSyncRepository>()
            .AddScoped<IGuildTicketConfigRepository, GuildTicketConfigRepository>()
            .AddScoped<IGuildTicketRepository, GuildTicketRepository>();

        // repositories
        //services
        //    .AddScoped<IWarnStrikeRepository, WarnStrikeRepository>()
        //    .AddScoped<IWarnRepository, WarnRepository>();
    }
}
