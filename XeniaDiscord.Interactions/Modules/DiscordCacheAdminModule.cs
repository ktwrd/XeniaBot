using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.Diagnostics;
using XeniaBot.Shared;
using XeniaDiscord.Common.Services;

namespace XeniaDiscord.Interactions.Modules;

[Group("cacheadmin", "Cache administration")]
public class DiscordCacheAdminModule : InteractionModuleBase
{
    private readonly ConfigData _config;
    private readonly DiscordCacheService _discordCacheService;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public DiscordCacheAdminModule(IServiceProvider services)
    {
        _config = services.GetRequiredService<ConfigData>();
        _discordCacheService = services.GetRequiredService<DiscordCacheService>();
    }

    [SlashCommand("update-current-guild", "Update cache for current guild")]
    public async Task UpdateCurrentGuild()
    {
        if (!_config.UserWhitelist.Contains(Context.User.Id))
        {
            await Context.Interaction.RespondAsync("Invalid permissions.");
            return;
        }
        else if (!Context.Interaction.GuildId.HasValue || Context.Guild == null)
        {
            await Context.Interaction.RespondAsync("This command must be executed in a guild.");
            return;
        }

        await DeferAsync();
        try
        {
            var sw = new Stopwatch();
            sw.Start();
            await _discordCacheService.UpdateGuild(Context.Guild);
            sw.Stop();
            var duration = Math.Round(sw.Elapsed.TotalMilliseconds / 1000f, 3);
            await Context.Interaction.FollowupAsync($"Done! Took {duration}s");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to update Guild \"{Context.Guild.Name}\" ({Context.Guild.Id})");
            await Context.Interaction.FollowupWithFileAsync(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ex.ToString())),
                "exception.txt",
                "Failed to update current guild.");
        }
    }
}
