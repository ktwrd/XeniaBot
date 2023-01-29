using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using ShortcakeBot.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortcakeBot.Core.Modules
{
    public class MiscModule : InteractionModuleBase
    {
        [SlashCommand("info", "Information about Shortcake")]
        public async Task Info()
        {
            var client = Program.Services.GetRequiredService<DiscordSocketClient>();
            var embed = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = client.CurrentUser.Username,
                    IconUrl = client.CurrentUser.GetAvatarUrl()
                },
                Timestamp = DateTimeOffset.UtcNow,
                Description = "Heya I'm Shortcake, a general-purpose Discord Bot made by [kate](https://kate.pet). If you're having any issues with using Shortcake, don't hesitate to open a [Git Issue](https://github.com/ktwrd/shortcake-issues/issues)."
            };
            embed.AddField("Uptime", DiscordHelper.GetUptimeString(), true);
            embed.AddField("Latency", $"{client.Latency}ms", true);
            await Context.Interaction.RespondAsync(embed: embed.Build());
        }
    }
}
