using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Controllers.BotAdditions;
using XeniaBot.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using XeniaBot.Data.Controllers.BotAdditions;

namespace XeniaBot.Core.Modules
{
    [Discord.Interactions.Group("confessadmin", "Administrative tools for the Confession Module")]
    public class ConfessionAdminModule : InteractionModuleBase
    {
        [SlashCommand("purge", "Remove all traces of the Confession Module from this guild")]
        [Discord.Interactions.RequireUserPermission(ChannelPermission.ManageChannels)]
        public async Task Purge()
        {
            var controller = Program.Core.GetRequiredService<ConfessionConfigController>();
            var item = await controller.GetGuild(Context.Guild.Id);
            if (item == null)
            {
                await Context.Interaction.RespondAsync("Guild not registered in database");
                return;
            }

            try
            {
                await controller.Delete(item);
            }
            catch (Exception ex)
            {
                await Context.Interaction.RespondAsync($"Failed to delete from database\n```\n{ex.Message}\n```");
                await DiscordHelper.ReportError(ex, Context);
                return;
            }
            await Context.Interaction.RespondAsync("Deleted Guild from Database");
        }

        [SlashCommand("set", "Setup the Confession Module")]
        [Discord.Interactions.RequireUserPermission(ChannelPermission.ManageChannels)]
        public async Task Set(
            [Discord.Interactions.Summary(name: "output-channel", description: "Channel where confessions will be sent to")]
            [ChannelTypes(ChannelType.Text)]
            IChannel confessionOutputChannel,
            [ChannelTypes(ChannelType.Text)]
            [Discord.Interactions.Summary(name: "modal-channel", description: "Channel where users will interact with a modal to create a confession.")]
            IChannel confessionModalChannel)
        {
            if (await DiscordHelper.HasGuildPermission(Context, GuildPermission.ManageChannels, true) == false)
                return;

            if (confessionOutputChannel.Id == confessionModalChannel.Id)
            {
                await Context.Interaction.RespondAsync("Output Channel and Modal Channel cannot be the same", ephemeral: true);
                return;
            }

            var controller = Program.Core.GetRequiredService<ConfessionController>();
            var config = Program.Core.GetRequiredService<ConfessionConfigController>();
            var item = await config.GetGuild(Context.Guild.Id);
            if (item != null)
            {
                await config.Delete(item);
            }
            await controller.InitializeModal(
                Context.Guild.Id,
                confessionOutputChannel.Id,
                confessionModalChannel.Id);
            await Context.Interaction.RespondAsync($"Done! See <#{confessionOutputChannel.Id}> to see where the confessions get sent to, and use the button in <#{confessionModalChannel.Id}> to add a confession.");
        }
    }
}
