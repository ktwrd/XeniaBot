using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared.Helpers;
using XeniaDiscord.Interactions.DataMigration.Modules;

namespace XeniaDiscord;

public static class XeniaDiscordInteractionsDataMigration
{
    public static void RegisterServices(IServiceCollection services)
    {
    }
    public static async Task RegisterModules(InteractionService interactions, IServiceProvider services)
    {
        var transaction = SentryHelper.CreateTransaction();
        await Task.WhenAll(
            interactions.AddModuleAsync<DataMigrationModule>(services)
            );
        transaction.Finish();
    }
}
