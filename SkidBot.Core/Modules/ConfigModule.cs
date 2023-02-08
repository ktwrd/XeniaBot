using Discord;
using Discord.Interactions;
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
        [SlashCommand("ticket", "Configure the Ticket Module")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task TicketConfig(
            [ChannelTypes(ChannelType.Category)] ICategoryChannel ticketCategory = null,
            IRole managerRole = null)
        {
            await Context.Interaction.RespondAsync(".");
        }
    }
}
