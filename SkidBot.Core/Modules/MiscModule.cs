using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SkidBot.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flurl.Util;
using SkidBot.Core.Controllers;
using SkidBot.Shared;

namespace SkidBot.Core.Modules
{
    public class MiscModule : InteractionModuleBase
    {
        [SlashCommand("info", "Information about Skid")]
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
                Description = "Heya I'm Skid, a general-purpose Discord Bot made by [kate](https://kate.pet). If you're having any issues with using Skid, don't hesitate to open a [Git Issue](https://github.com/ktwrd/skidbot-issues/issues)."
            };
            embed.AddField("Statistics", string.Join("\n", new string[]
            {
                $"Guilds: {client.Guilds.Count}",
                $"Latency: {client.Latency}ms",
                $"Uptime: {DiscordHelper.GetUptimeString()}"
            }));
            await Context.Interaction.RespondAsync(embed: embed.Build());
        }

        [SlashCommand("metricreload", "Reload Prometheus Metrics")]
        [RequireOwner]
        public async Task ReloadMetrics()
        {
            var config = Program.Services.GetRequiredService<SkidConfig>();
            if (!config.Prometheus_Enable)
            {
                await Context.Interaction.RespondAsync("Prometheus exporter is disabled", ephemeral: true);
                return;
            }

            var prom = Program.Services.GetRequiredService<PrometheusController>();
            if (prom == null)
            {
                await Context.Interaction.RespondAsync(
                    "Failed to get required service \"PrometheusController\" since it's null.",
                    ephemeral: true);
                return;
            }

            try
            {
                prom.OnReloadMetrics();
            }
            catch (Exception e)
            {
                await Context.Interaction.RespondAsync(
                    $"```\n{e.Message}\n```",
                    ephemeral: true);
                throw;
            }

            await Context.Interaction.RespondAsync("Done!", ephemeral: true);
        }
    }
}
