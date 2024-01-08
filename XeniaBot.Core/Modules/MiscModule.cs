using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Flurl.Util;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using XeniaBot.Core.Controllers;
using XeniaBot.Data.Controllers;
using XeniaBot.Shared;
using XeniaBot.Shared.Controllers;

namespace XeniaBot.Core.Modules
{
    public class MiscModule : InteractionModuleBase
    {
        [SlashCommand("info", "Information about Xenia")]
        public async Task Info()
        {
            var client = Program.Services.GetRequiredService<DiscordSocketClient>();
            var embed = DiscordHelper.BaseEmbed()
                .WithDescription(string.Join(" ", new string[]
                {
                    "Heya I'm Xenia, a general-purpose Discord Bot made by [kate](https://kate.pet).",
                    "If you're having any issues with using Xenia, don't hesitate to open a [Git Issue](https://github.com/ktwrd/xeniabot/issues)."
                }))
                .AddField("Statistics", string.Join("\n", new string[]
                {
                    "```",
                    $"Guilds:     {client.Guilds.Count}",
                    $"Latency:    {client.Latency}ms",
                    $"Uptime:     {DiscordHelper.GetUptimeString()}",
                    $"Version:    {Program.Version}",
                    $"Build Date: {Program.VersionDate}",
                    "```"
                }))
                .WithColor(new Color(255, 255, 255));
            await Context.Interaction.RespondAsync(embed: embed.Build());
        }

        [SlashCommand("dashboard", "Fetch Dashboard information")]
        public async Task Dashboard()
        {
            if (Program.ConfigData.HasDashboard)
            {
                await Context.Interaction.RespondAsync(embed: new EmbedBuilder()
                    .WithTitle("Xenia Dashboard")
                    .WithDescription($"The dashboard is publicly accessible at {Program.ConfigData.DashboardUrl}")
                    .WithColor(Color.Blue)
                    .WithCurrentTimestamp()
                    .Build());
            }
            else
            {
                await Context.Interaction.RespondAsync(embed: new EmbedBuilder()
                    .WithTitle("Xenia Dashboard")
                    .WithDescription($"Unfortunately, the dashboard has not been setup yet. Please wait for the Xenia Dashboard to become publicly available.\n\nTo be the first to know, [join our discord server](https://r.kate.pet/discord)!")
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp()
                    .Build());
            }
        }

        [SlashCommand("metricreload", "Reload Prometheus Metrics")]
        public async Task ReloadMetrics()
        {
            if (!Program.ConfigData.UserWhitelist.Contains(Context.User.Id))
            {
                await Context.Interaction.FollowupAsync("You do not have permission to access this command");
                return;
            }
            var config = Program.Services.GetRequiredService<ConfigData>();
            var embed = DiscordHelper.BaseEmbed()
                .WithTitle("Reload Prometheus Metrics");
            
            if (!config.Prometheus.Enable)
            {
                await Context.Interaction.RespondAsync(
                    embed: embed.WithDescription("Prometheus Exporter is disabled").Build());
                return;
            }

            var prom = Program.Services.GetRequiredService<PrometheusController>();
            if (prom == null)
            {
                await Context.Interaction.RespondAsync(
                    embed: embed.WithDescription("Failed to get required service \"PrometheusController\" since it's null.").Build());
                return;
            }

            try
            {
                prom.OnReloadMetrics();
            }
            catch (Exception e)
            {
                await Context.Interaction.RespondAsync(
                    embed: embed
                        .WithDescription($"```\n{e.Message}\n```")
                        .WithTitle("Failed to Reload Prometheus Metrics")
                        .Build());
                throw;
            }

            await Context.Interaction.RespondAsync(
                embed: embed.WithDescription("Done!").Build());
        }

        [SlashCommand("invite", "Get invite link for Xenia")]
        public async Task Invite()
        {
            var config = Program.Services.GetRequiredService<ConfigData>();
            var inviteLink =
                $"https://discord.com/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&scope=bot&permissions={config.InvitePermissions}";
            await Context.Interaction.RespondAsync(embed: DiscordHelper.BaseEmbed()
                .WithUrl(inviteLink)
                .WithTitle("Invite Xenia")
                .WithColor(Color.DarkGreen)
                .WithDescription(
                    "To invite Xenia to your own server, click on the \"Invite Xenia\" link and it should take you to Discord's Website to invite Xenia to any of your servers!")
                .Build());
        }

        [SlashCommand("fetch_config", "Fetch data from config file")]
        public async Task FetchConfig()
        {
            if (!Program.ConfigData.UserWhitelist.Contains(Context.User.Id))
            {
                await Context.Interaction.FollowupAsync("You do not have permission to access this command");
                return;
            }
            var config = Program.Services.GetRequiredService<ConfigData>();
            var fileContent = JsonSerializer.Serialize(config, Program.SerializerOptions);
            await Context.Interaction.RespondWithFileAsync(
                new MemoryStream(Encoding.UTF8.GetBytes(fileContent)), 
                "config.json",
                "Attached as JSON",
                ephemeral: true);
        }

        [SlashCommand("dadjoke", "Ya know, jokes that your dad would make?")]
        public async Task DadJoke()
        {
            try
            {
                await DeferAsync();   
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", $"Xenia Bot (https://github.com/ktwrd/xeniabot)");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                var response = await client.GetAsync("https://icanhazdadjoke.com/");
                var text = response.Content.ReadAsStringAsync().Result;
                var deser = JObject.Parse(text);
                if (deser?["joke"] != null)
                {
                    await FollowupAsync(deser["joke"].ToString());
                }
                else
                {
                    await FollowupAsync("`Failed to get dad joke ;w;`");
                    await Program.Services.GetRequiredService<ErrorReportController>().ReportError(response, Context);
                }
            }
            catch (Exception ex)
            {
                await FollowupAsync($"`Failed to get dad joke ;w; ({ex.Message})`");
                await Program.Services.GetRequiredService<ErrorReportController>().ReportError(ex, Context);
            }
        }
    }
}
