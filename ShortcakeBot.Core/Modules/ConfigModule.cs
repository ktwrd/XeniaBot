using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortcakeBot.Core.Modules
{
    [Discord.Interactions.Group("config", "Configure Shortcake")]
    public class ConfigModule : InteractionModuleBase
    {
        [SlashCommand("ticket", "Configure the Ticket Module")]
        public async Task TicketConfig(
            [ChannelTypes(ChannelType.Category)] ICategoryChannel ticketCategory = null,
            IRole managerRole = null)
        {
            await Context.Interaction.RespondAsync(".");
        }
    }
}
