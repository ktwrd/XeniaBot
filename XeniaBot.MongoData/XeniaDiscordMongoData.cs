using Microsoft.Extensions.DependencyInjection;
using XeniaBot.MongoData.Repositories;

// ReSharper disable CheckNamespace
#pragma warning disable IDE0130
#pragma warning disable S1186

namespace XeniaDiscord;

public class XeniaDiscordMongoData
{
    public static void RegisterServices(IServiceCollection services)
    {
        services
            .AddSingleton<BanSyncConfigRepository>()
            .AddSingleton<BanSyncInfoRepository>()
            .AddSingleton<BanSyncStateHistoryRepository>()
            .AddSingleton<RoleConfigRepository>()
            .AddSingleton<RoleMessageConfigRepository>()
            .AddSingleton<ConfessionConfigRepository>()
            .AddSingleton<CounterConfigRepository>()
            .AddSingleton<EconomyProfileRepository>()
            .AddSingleton<ESixConfigRepository>()
            .AddSingleton<GuildConfigWarnStrikeRepository>()
            .AddSingleton<GuildGreetByeConfigRepository>()
            .AddSingleton<GuildGreeterConfigRepository>()
            .AddSingleton<GuildWarnItemRepository>()
            .AddSingleton<LevelMemberRepository>()
            .AddSingleton<LevelSystemConfigRepository>()
            .AddSingleton<ReminderRepository>()
            .AddSingleton<RolePreserveGuildRepository>()
            .AddSingleton<RolePreserveRepository>()
            .AddSingleton<ServerLogRepository>()
            .AddSingleton<UserConfigRepository>()
            .AddSingleton<XeniaVersionRepository>();
    }
}