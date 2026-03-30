using Discord.Interactions;
using XeniaBot.Shared.Helpers;
using XeniaDiscord.Interactions.Modules;

namespace XeniaDiscord;

public static class XeniaDiscordInteractions
{
    public static async Task RegisterModules(InteractionService interactions, IServiceProvider services)
    {
        var transaction = SentryHelper.CreateTransaction();
        var types = new Type[]
        {
            typeof(GuildApprovalModule),
            typeof(GuildApprovalModalModule),
            typeof(GuildApprovalAdminModule),
        };
        await Task.WhenAll(types.Select(type => interactions.AddModuleAsync(type, services)));
        transaction.Finish();
    }
    public static async Task<ModuleInfo[]> RegisterDeveloperModules(InteractionService interactions, IServiceProvider services)
    {
        var transaction = SentryHelper.CreateTransaction();
        var types = new[]
        {
            typeof(DiscordCacheAdminModule),
            typeof(DeveloperModule)
        };
        var result = await Task.WhenAll(types.Select(type => interactions.AddModuleAsync(type, services)));
        transaction.Finish();
        return result;
    }
}
