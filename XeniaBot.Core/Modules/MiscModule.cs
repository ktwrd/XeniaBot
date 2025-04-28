using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using XeniaBot.Core.Helpers;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;

namespace XeniaBot.Core.Modules
{
    public class MiscModule : InteractionModuleBase
    {
        private CoreContext _core => CoreContext.Instance!;
        [SlashCommand("info", "Information about Xenia")]
        public async Task Info()
        {
            var client = _core.GetRequiredService<DiscordSocketClient>();
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
                    $"Version:    {_core.Details.Version}",
                    $"Build Date: {_core.Details.VersionDate}",
                    "```"
                }))
                .WithColor(new Color(255, 255, 255));
            await Context.Interaction.RespondAsync(embed: embed.Build());
        }

        [SlashCommand("dashboard", "Fetch Dashboard information")]
        public async Task Dashboard()
        {
            if (_core.Config.Data.HasDashboard)
            {
                await Context.Interaction.RespondAsync(embed: new EmbedBuilder()
                    .WithTitle("Xenia Dashboard")
                    .WithDescription($"The dashboard is publicly accessible at {_core.Config.Data.DashboardUrl}")
                    .WithColor(Color.Blue)
                    .WithCurrentTimestamp()
                    .Build());
            }
            else
            {
                await Context.Interaction.RespondAsync(embed: new EmbedBuilder()
                    .WithTitle("Xenia Dashboard")
                    .WithDescription($"Unfortunately, the dashboard has not been setup yet. Please wait for the Xenia Dashboard to become publicly available.\n\nTo be the first to know, [join our discord server](https://kate.pet/l/discord)!")
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp()
                    .Build());
            }
        }

        [SlashCommand("metricreload", "Reload Prometheus Metrics")]
        public async Task ReloadMetrics()
        {
            if (!_core.Config.Data.UserWhitelist.Contains(Context.User.Id))
            {
                await Context.Interaction.FollowupAsync("You do not have permission to access this command");
                return;
            }
            var config = _core.GetRequiredService<ConfigData>();
            var embed = DiscordHelper.BaseEmbed()
                .WithTitle("Reload Prometheus Metrics");
            
            if (!config.Prometheus.Enable)
            {
                await Context.Interaction.RespondAsync(
                    embed: embed.WithDescription("Prometheus Exporter is disabled").Build());
                return;
            }

            var prom = _core.GetRequiredService<PrometheusService>();
            if (prom == null)
            {
                await Context.Interaction.RespondAsync(
                    embed: embed.WithDescription("Failed to get required service \"PrometheusService\" since it's null.").Build());
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
            var config = _core.GetRequiredService<ConfigData>();
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
            if (!_core.Config.Data.UserWhitelist.Contains(Context.User.Id))
            {
                await Context.Interaction.FollowupAsync("You do not have permission to access this command");
                return;
            }
            var config = _core.GetRequiredService<ConfigData>();
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
                if (deser?.TryGetValue("joke", out var jokeValue) ?? false && jokeValue != null)
                {
                    await FollowupAsync(jokeValue.ToString());
                }
                else
                {
                    await FollowupAsync("`Failed to get dad joke ;w;`");
                    await _core.GetRequiredService<ErrorReportService>().ReportError(response, Context);
                }
            }
            catch (Exception ex)
            {
                await FollowupAsync($"`Failed to get dad joke ;w; ({ex.Message})`");
                await _core.GetRequiredService<ErrorReportService>().ReportError(ex, Context);
            }
        }
    }
}
