using Microsoft.Extensions.DependencyInjection;
using XeniaBot.DiscordCache.Repositories;

// ReSharper disable CheckNamespace
#pragma warning disable IDE0130
#pragma warning disable S1186

namespace XeniaDiscord;

public static class XeniaMongoDiscordCache
{
    public static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<UserCacheRepository>()
            .AddSingleton<MessageCacheRepository>();
    }
}