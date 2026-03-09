using Discord.Interactions;
using XeniaBot.Shared.Helpers;
using XeniaDiscord.Interactions.Modules;

namespace XeniaDiscord;

public static class XeniaDiscordInteractions
{
    public static async Task RegisterModules(InteractionService interactions, IServiceProvider services)
    {
        var transaction = SentryHelper.CreateTransaction();
        await Task.WhenAll(
            interactions.AddModuleAsync<DiscordCacheAdminModule>(services)
            );
        transaction.Finish();
    }
}
