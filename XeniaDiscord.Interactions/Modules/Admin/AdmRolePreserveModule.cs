using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.Diagnostics;
using System.Text;
using XeniaBot.Shared;
using XeniaDiscord.Common.Services;

namespace XeniaDiscord.Interactions.Modules.Admin;

[DeveloperModule]
[Group("adm-rp", "Admin: Role Preserve")]
[CommandContextType(InteractionContextType.Guild)]
[RequireDeveloper]
public class AdmRolePreserveModule : InteractionModuleBase
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly RolePreserveService _service;
    private readonly ConfigData _config;
    public AdmRolePreserveModule(IServiceProvider services)
    {
        _service = services.GetRequiredService<RolePreserveService>();
        _config = services.GetRequiredService<ConfigData>();
    }

    [SlashCommand("preserve-all", "re-seed rolepreserve db in all guilds")]
    public async Task PreserveAll()
    {
        if (!_config.UserWhitelist.Contains(Context.User.Id)) return;

        await DeferAsync();
        var sw = new Stopwatch();
        sw.Start();
        try
        {
            await _service.PreserveAll();
            sw.Stop();
            var duration = Math.Round(sw.Elapsed.TotalMilliseconds / 1000f, 3);
            var count = Context.Client.GetGuildsAsync().GetAwaiter().GetResult().Count;
            await FollowupAsync($"Took {duration}s to preserve all roles in {count} guild(s)");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to preserve all guilds");
            await Context.Interaction.FollowupWithFileAsync(
                new MemoryStream(Encoding.UTF8.GetBytes(ex.ToString())),
                "exception.txt",
                "Failed to preserve all guilds.");
        }
    }
}
