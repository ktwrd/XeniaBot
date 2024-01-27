using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Controllers.BotAdditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Data.Models;

namespace XeniaBot.Core.Modules
{
    [Discord.Interactions.Group("config", "Configure Xenia")]
    public class ConfigModule : InteractionModuleBase
    {
        [SlashCommand("ticket", "Configure the Ticket Module. To view the current config, run with no options.")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task TicketConfig(
            [Discord.Interactions.Summary(description: "Parent Category for all channels that will be created when a ticket is made.")]
            [ChannelTypes(ChannelType.Category)] ICategoryChannel ticketCategory = null,
            [Discord.Interactions.Summary(description: "Role for who will be able to manage tickets.")]
            IRole managerRole = null,
            [Discord.Interactions.Summary(description: "Channel where ticket states will be logged and archived.")]
            [ChannelTypes(ChannelType.Text)] ITextChannel logChannel = null)
        {
            TicketController controller = Program.Core.GetRequiredService<TicketController>();

            ConfigGuildTicketModel? model = await controller.GetGuildConfig(Context.Guild.Id);
            if (model == null)
            {
                model = new ConfigGuildTicketModel()
                {
                    GuildId = Context.Guild.Id
                };
            }

            bool updateCategory = ticketCategory != null;
            bool updateRole = managerRole != null;
            bool updateChannel = logChannel != null;
            model.CategoryId = ticketCategory?.Id ?? model.CategoryId;
            model.RoleId = managerRole?.Id ?? model.RoleId;
            model.LogChannelId = logChannel?.Id ?? model.LogChannelId;

            var content = new List<string>();
            if (updateCategory)
                content.Add($"Ticket Category to <#{ticketCategory.Id}>");
            if (updateRole)
                content.Add($"Manager Role to <@&{managerRole.Id}>");
            if (updateChannel)
                content.Add($"Logging Channel to <#{logChannel.Id}>");

            var embed = new EmbedBuilder()
            {
                Title = "Ticket Config",
                Color = new Color(255, 255, 255)
            };

            embed.AddField("Current Config", string.Join("\n", new string[]
            {
                    $"Channel Category: <#{model.CategoryId}> (`{model.CategoryId}`)",
                    $"Manager Role: <@&{model.RoleId}> (`{model.RoleId}`)",
                    $"Log: <#{model.LogChannelId}> (`{model.LogChannelId}`)"
            }));
            if (content.Count > 0)
            {
                embed.Description = "Updated " + string.Join(" and ", content);
            }
            await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);

            await controller.SetGuildConfig(model);
        }
    }
}
