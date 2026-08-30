using Discord.Interactions;
using System;
using System.Threading.Tasks;
using XeniaBot.Core.LevelSystem.Modules;
using XeniaBot.Core.Modules;
using XeniaBot.Shared.Helpers;
// ReSharper disable CheckNamespace
#pragma warning disable IDE0130
#pragma warning disable S1186

namespace XeniaDiscord;

public static class XeniaDiscordCoreInteractions
{
    public static async Task RegisterModules(InteractionService interactions, IServiceProvider services)
    {
        var transaction = SentryHelper.CreateTransaction();
        await Task.WhenAll(
            interactions.AddModuleAsync<MediaManipulationModule>(services),
            interactions.AddModuleAsync<BackpackTFModule>(services),
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
            interactions.AddModuleAsync<TicketModule>(services),
            interactions.AddModuleAsync<TranslateModule>(services),
            interactions.AddModuleAsync<WeatherModule>(services),
            interactions.AddModuleAsync<XpModule>(services)
            );
        transaction.Finish();
    }
}
