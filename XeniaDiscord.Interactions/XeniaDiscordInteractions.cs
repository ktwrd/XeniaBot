using Discord.Interactions;
using XeniaBot.Shared.Helpers;
using XeniaDiscord.Interactions.Modules;
using XeniaDiscord.Interactions.Modules.Admin;
// ReSharper disable CheckNamespace
#pragma warning disable IDE0130
#pragma warning disable S1186

namespace XeniaDiscord;

public static class XeniaDiscordInteractions
{
    public static async Task RegisterModules(InteractionService interactions, IServiceProvider services)
    {
        var transaction = SentryHelper.CreateTransaction();
        var types = new[]
        {
            typeof(BanSyncModule),
            typeof(GuildApprovalModule),
            typeof(GuildApprovalModalModule),
            typeof(GuildApprovalAdminModule),
            typeof(RolePreserveComponentModule),
            typeof(RolePreserveModule),
            typeof(ServerLogModule),
        };
        await Task.WhenAll(types.Select(type => interactions.AddModuleAsync(type, services)));
        transaction.Finish();
    }

    public static async Task<ModuleInfo[]> RegisterDeveloperModules(InteractionService interactions, IServiceProvider services)
    {
        var transaction = SentryHelper.CreateTransaction();
        var types = new[]
        {
            typeof(AdmRolePreserveModule),

            typeof(AdmDataModule),
            typeof(DeveloperModule)
        };
        var result = await Task.WhenAll(types.Select(type => interactions.AddModuleAsync(type, services)));
        transaction.Finish();
        return result;
    }
}
