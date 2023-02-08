using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using SkidBot.Core.Controllers;
using SkidBot.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Modules
{
    [Discord.Interactions.Group("config", "Configure Skid")]
    public class ConfigModule : InteractionModuleBase
    {
        [SlashCommand("ticket", "Configure the Ticket Module. To view the current config, run with no options.")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task TicketConfig(
            [ChannelTypes(ChannelType.Category)] ICategoryChannel ticketCategory = null,
            IRole managerRole = null)
        {
            TicketController controller = Program.Services.GetRequiredService<TicketController>();

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
            model.CategoryId = ticketCategory?.Id ?? model.CategoryId;
            model.RoleId = managerRole?.Id ?? model.RoleId;

            var content = new List<string>();
            if (updateCategory)
                content.Add($"Ticket Category to <#{ticketCategory.Id}>");
            if (updateRole)
                content.Add($"Manager Role to <@&{managerRole.Id}>");

            var embed = new EmbedBuilder()
            {
                Title = "Ticket Config",
                Color = new Color(255, 255, 255)
            };

            if (!updateCategory && !updateRole)
            {
                embed.Description = string.Join("\n", new string[]
                {
                    "Current Config",
                    $"<#{model.CategoryId}> (`{model.CategoryId}`)",
                    $"<@&{model.RoleId}> (`{model.RoleId}`)"
                });
            }
            else
            {
                embed.Description = "Updated " + string.Join(" and ", content);
            }
            await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);

            await controller.SetGuildConfig(model);
        }
    }
}
