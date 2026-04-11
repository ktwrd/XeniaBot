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
using Microsoft.Extensions.DependencyInjection;

namespace XeniaBot.Core.Modules;

public class MiscModule : InteractionModuleBase
{
    private readonly DiscordSocketClient _client;
    private readonly ConfigData _config;
    private readonly ProgramDetails _details;
    private readonly PrometheusService _prometheus;
    private readonly ErrorReportService _error;
    public MiscModule(IServiceProvider services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        _config = services.GetRequiredService<ConfigData>();
        _details = services.GetRequiredService<ProgramDetails>();
        _prometheus = services.GetRequiredService<PrometheusService>();
        _error = services.GetRequiredService<ErrorReportService>();
    }

    [SlashCommand("info", "Information about Xenia")]
    [RegisterDBLCommand]
    public async Task Info()
    {
        var embed = DiscordHelper.BaseEmbed()
            .WithDescription(string.Join(" ",
                "Heya I'm Xenia, a general-purpose Discord Bot made by [kate](https://kate.pet).",
                "If you're having any issues with using Xenia, don't hesitate to open a [Git Issue](https://github.com/ktwrd/xeniabot/issues)."
            ))
            .AddField("Statistics", string.Join("\n",
                "```",
                $"Guilds:     {_client.Guilds.Count}",
                $"Latency:    {_client.Latency}ms",
                $"Uptime:     {DiscordHelper.GetUptimeString()}",
                $"Version:    {_details.Version}",
                $"Build Date: {_details.VersionDate}",
                "```"
            ))
            .WithColor(new Color(255, 255, 255));
        await Context.Interaction.RespondAsync(embed: embed.Build());
    }

    [SlashCommand("dashboard", "Fetch Dashboard information")]
    [RegisterDBLCommand]
    public async Task Dashboard()
    {
        if (_config.HasDashboard)
        {
            await Context.Interaction.RespondAsync(embed: new EmbedBuilder()
                .WithTitle("Xenia Dashboard")
                .WithDescription($"The dashboard is publicly accessible at {_config.DashboardUrl}")
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
        if (!_config.UserWhitelist.Contains(Context.User.Id))
        {
            await Context.Interaction.FollowupAsync("You do not have permission to access this command");
            return;
        }
        var embed = DiscordHelper.BaseEmbed()
            .WithTitle("Reload Prometheus Metrics");
        
        if (!_config.Prometheus.Enable)
        {
            await Context.Interaction.RespondAsync(
                embed: embed.WithDescription("Prometheus Exporter is disabled").Build());
            return;
        }

        try
        {
            _prometheus.OnReloadMetrics();
        }
        catch (Exception e)
        {
            await Context.Interaction.RespondAsync(
                embed: embed
                    .WithDescription(e.Message[..Math.Min(4096, e.Message.Length)])
                    .WithTitle("Failed to Reload Prometheus Metrics")
                    .Build());
            throw;
        }

        await Context.Interaction.RespondAsync(
            embed: embed.WithDescription("Done!").Build());
    }

    [SlashCommand("invite", "Get invite link for Xenia")]
    [RegisterDBLCommand]
    public async Task Invite()
    {
        var inviteLink =
            $"https://discord.com/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&scope=bot&permissions={_config.InvitePermissions}";
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
        if (!_config.UserWhitelist.Contains(Context.User.Id))
        {
            await Context.Interaction.FollowupAsync("You do not have permission to access this command");
            return;
        }
        var fileContent = JsonSerializer.Serialize(_config, Program.SerializerOptions);
        await Context.Interaction.RespondWithFileAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(fileContent)), 
            "config.json",
            "Attached as JSON",
            ephemeral: true);
    }

    [SlashCommand("dadjoke", "Ya know, jokes that your dad would make?")]
    [RegisterDBLCommand]
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
                await _error.ReportError(response, Context);
            }
        }
        catch (Exception ex)
        {
            await FollowupAsync($"`Failed to get dad joke ;w; ({ex.Message})`");
            await _error.ReportError(ex, Context);
        }
    }
}
