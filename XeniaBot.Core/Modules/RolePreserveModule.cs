using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using XeniaBot.Core.Helpers;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;

namespace XeniaBot.Core.Modules;

[Discord.Interactions.Group("rolepreserve", "Configure the RolePreserve module.")]
public class RolePreserveModule : InteractionModuleBase
{
    [SlashCommand("enable", "Grant members preserved roles on re-join.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task Enable()
    {
        try
        {
            var controller = Program.Core.GetRequiredService<RolePreserveGuildRepository>();
            var data = await controller.Get(Context.Guild.Id) ?? new RolePreserveGuildModel()
            {
                GuildId = Context.Guild.Id
            };
            data.Enable = true;
            await controller.Set(data);
            
            await RespondAsync(embed: new EmbedBuilder()
                .WithTitle("Role Preserve")
                .WithDescription($"Roles will now be granted to members when they re-join (if Xenia can grant those roles).")
                .Build());
        }
        catch (Exception ex)
        {
            await RespondAsync(embed: new EmbedBuilder()
                .WithTitle("Role Preserve - Failed to Enable")
                .WithDescription($"Failed to enable Role Preserve feature. `{ex.Message}`")
                .WithColor(Color.Red)
                .Build());
            await DiscordHelper.ReportError(ex, Context);
        }
    }

    [SlashCommand("disable", "Disable Role Grant feature.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task Disable()
    {
        try
        {
            var controller = Program.Core.GetRequiredService<RolePreserveGuildRepository>();
            var data = await controller.Get(Context.Guild.Id) ?? new RolePreserveGuildModel()
            {
                GuildId = Context.Guild.Id
            };
            data.Enable = false;
            await controller.Set(data);
            
            await RespondAsync(embed: new EmbedBuilder()
                .WithTitle("Role Preserve")
                .WithDescription($"Xenia will continue to track member roles, but they will not be granted on re-join.")
                .Build());
        }
        catch (Exception ex)
        {
            await RespondAsync(embed: new EmbedBuilder()
                .WithTitle("Role Preserve - Failed to Disable")
                .WithDescription($"Failed to disable Role Preserve feature. `{ex.Message}`")
                .WithColor(Color.Red)
                .Build());
            await DiscordHelper.ReportError(ex, Context);
        }
    }
}