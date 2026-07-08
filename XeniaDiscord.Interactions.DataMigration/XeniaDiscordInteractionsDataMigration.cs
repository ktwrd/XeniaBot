using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared.Helpers;
using XeniaDiscord.Interactions.DataMigration.Modules;
// ReSharper disable CheckNamespace
#pragma warning disable IDE0130
#pragma warning disable S1186

namespace XeniaDiscord;

public static class XeniaDiscordInteractionsDataMigration
{
    public static void RegisterServices(IServiceCollection services)
    {
    }
    
    public static async Task<ModuleInfo[]> RegisterDeveloperModules(InteractionService interactions, IServiceProvider services)
    {
        var transaction = SentryHelper.CreateTransaction();
        var types = new[]
        {
            typeof(DataMigrationModule),
        };
        var result = await Task.WhenAll(types.Select(type => interactions.AddModuleAsync(type, services)));
        transaction.Finish();
        return result;
    }
}
