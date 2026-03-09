using Discord.Interactions;
using System;
using System.Threading.Tasks;
using XeniaBot.Core.Modules;
using XeniaBot.Shared.Helpers;

namespace XeniaDiscord;

public static class XeniaDiscordInteractions
{
    public static async Task RegisterModules(InteractionService interactions, IServiceProvider services)
    {
        var transaction = SentryHelper.CreateTransaction();
        await Task.WhenAll(
            interactions.AddModuleAsync<MediaManipulationModule>(services),
            interactions.AddModuleAsync<BackpackTFModule>(services),
            interactions.AddModuleAsync<BanSyncModule>(services),
            interactions.AddModuleAsync<ConfessionAdminModule>(services),
            interactions.AddModuleAsync<ConfigModule>(services),
            interactions.AddModuleAsync<CounterModule>(services),
            interactions.AddModuleAsync<DiceModule>(services),
            interactions.AddModuleAsync<DistroWatchModule>(services),
            interactions.AddModuleAsync<EconomyModule>(services),
            interactions.AddModuleAsync<HelpModule>(services),
            interactions.AddModuleAsync<MiscModule>(services),
            interactions.AddModuleAsync<ModerationModule>(services),
            interactions.AddModuleAsync<RandomAnimalModule>(services),
            interactions.AddModuleAsync<ReminderModule>(services),
            interactions.AddModuleAsync<RolePreserveModule>(services),
            interactions.AddModuleAsync<ServerLogModule>(services),
            interactions.AddModuleAsync<TicketModule>(services),
            interactions.AddModuleAsync<TranslateModule>(services),
            interactions.AddModuleAsync<WeatherModule>(services)
            );
        transaction.Finish();
    }
}
