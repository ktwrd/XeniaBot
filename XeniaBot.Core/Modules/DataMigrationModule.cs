using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using XeniaBot.Shared;

namespace XeniaBot.Core.Modules;

[Group("data-migration", "Used for the MongoDB->PostgreSQL data migration.")]
public class DataMigrationModule : InteractionModuleBase
{
    private readonly ConfigData _config;
    public DataMigrationModule(IServiceProvider services)
    {
        _config = services.GetRequiredService<ConfigData>();
    }

    [SlashCommand("bansync", "Migrate all BanSync-related tables")]
    public async Task BanSync()
    {
        await DeferAsync();
        if (!_config.UserWhitelist.Contains(Context.User.Id))
        {
            await Context.Interaction.FollowupAsync("Invalid permissions.");
            return;
        }

        // TODO pull all data from mongodb, map it, then add it into postgres
    }
}
