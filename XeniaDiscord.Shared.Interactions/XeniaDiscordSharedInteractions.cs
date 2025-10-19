using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaDiscord.Shared.Interactions.Modules;

namespace XeniaDiscord.Shared.Interactions;

public static class XeniaDiscordSharedInteractions
{
    public static async Task RegisterModules(ForeverInteractionService interactions, IServiceProvider provider)
    {
        var transaction = SentryHelper.CreateTransaction();
        await Task.WhenAll(
            interactions.AddModuleAsync<BackpackTFModule>(provider),
            interactions.AddModuleAsync<BanSyncModule>(provider),
            interactions.AddModuleAsync<ConfessionAdminModule>(provider),
            interactions.AddModuleAsync<CounterModule>(provider),
            interactions.AddModuleAsync<DiceModule>(provider),
            interactions.AddModuleAsync<DistroWatchModule>(provider),
            interactions.AddModuleAsync<HelpModule>(provider),
            interactions.AddModuleAsync<MiscModule>(provider),
            interactions.AddModuleAsync<ModerationModule>(provider),
            interactions.AddModuleAsync<RandomAnimalModule>(provider),
            interactions.AddModuleAsync<ReminderModule>(provider),
            interactions.AddModuleAsync<RolePreserveModule>(provider),
            interactions.AddModuleAsync<ServerLogModule>(provider),
            interactions.AddModuleAsync<TicketModule>(provider),
            interactions.AddModuleAsync<TranslateModule>(provider),
            interactions.AddModuleAsync<WeatherModule>(provider));
        transaction.Finish();
    }
}
