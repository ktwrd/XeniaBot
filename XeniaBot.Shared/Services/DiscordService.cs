using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using XeniaBot.Shared.Helpers;

using Timer = System.Timers.Timer;
using LogSeverity = Discord.LogSeverity;

namespace XeniaBot.Shared.Services;

[XeniaController]
public class DiscordService
{
    private static readonly Logger Log = LogManager.GetLogger("Xenia.DiscordService");
    private readonly DiscordSocketClient _client;
    private readonly ConfigData _configData;
    private readonly InteractionHandler? _interactionHandler;
    private readonly PrometheusService _prom;
    private readonly ProgramDetails _details;
    public DiscordService(IServiceProvider services)
    {
        _details = services.GetRequiredService<ProgramDetails>();

        _configData = services.GetRequiredService<ConfigData>();
        _client = services.GetRequiredService<DiscordSocketClient>();

        if (_details.Platform == XeniaPlatform.Bot)
        {
            _interactionHandler = services.GetRequiredService<InteractionHandler>();
        }
        else
        {
            _interactionHandler = null;
        }

        _prom = services.GetRequiredService<PrometheusService>();

        // Prometheus Events
        _prom.ServerStart += InitializeMetrics;
        _client.SlashCommandExecuted += Event_SlashCommandExecuted_Prom;

        _client.Log += DiscordClientLogHandler;
        _client.Ready += _client_Ready;
    }

    public async Task Run()
    {
        _client.MessageReceived += (arg) =>
        {
            new Thread((ThreadStart)delegate
            {
                MessageReceived?.Invoke(arg);
            }).Start();
            return Task.CompletedTask;
        };

        await _client.LoginAsync(TokenType.Bot, _configData.DiscordToken);
        await _client.StartAsync();
    }

    #region Prometheus Metrics
    private Gauge? _promCount_GuildCount = null;
    private Gauge? _promCount_GuildUserCount = null;
    private Counter? _promCount_InteractionCount = null;
    private async Task InitializeMetrics()
    {
        if (!_configData.Prometheus.Enable)
        {
            Log.Warn("Ignoring, Prometheus Metrics are disabled");
            return;
        }
        _promCount_GuildCount = _prom.CreateGauge(
            "xenia_discord_guild_count",
            "Amount of guilds this bot is in",
            publish: false);
        _promCount_GuildUserCount = _prom.CreateGauge(
            "xenia_discord_guild_users",
            "Amount of users per guild",
            labelNames: new string[]
            {
                "guild_name",
                "guild_id"
            },
            publish: false);
        _promCount_InteractionCount = _prom.CreateCounter(
            "xenia_discord_interaction_count",
            "Interactions received",
            labelNames: new string[]
            {
                "guild_name",
                "guild_id",
                "author_name",
                "author_id",
                "channel_name",
                "channel_id",
                "interaction_name",
                "interaction_id"
            },
            publish: false);

        // Once metrics are setup, reload discord-related metrics
        await ReloadMetrics();
        Log.Debug("Created all metrics!");
        // Create timer
        CreateMetricTimer();

        _prom.ReloadMetrics += ReloadMetrics;
    }

    #region Metric Timer
    /// <summary>
    /// When to update metrics, measured in seconds.
    /// </summary>
    private const int MetricTimerInterval = 30;

    private readonly Timer _metricTimer = new Timer(MetricTimerInterval);
    private void CreateMetricTimer()
    {
        if (!_configData.Prometheus.Enable)
            return;
        
        _metricTimer.AutoReset = true;
        _metricTimer.Enabled = true;
        _metricTimer.Elapsed += async (sender, args) =>
        {
            await ReloadMetrics();
        };

        _metricTimer.Start();
    }
    #endregion

    #region Metrics (Triggered by Timer)
    public async Task ReloadMetrics()
    {
        if (!_configData.Prometheus.Enable)
            return;

        var taskList = new List<Task>()
        {
            ReloadMetrics_GuildCountSlow()
        };
        await Task.WhenAll(taskList);
    }
    /// <summary>
    /// Update <see cref="_promCount_GuildCount"/> and <see cref="_promCount_GuildUserCount"/>
    /// </summary>
    /// <exception cref="Exception">When <see cref="_promCount_GuildCount"/> or <see cref="_promCount_GuildUserCount"/> is null</exception>
    private async Task ReloadMetrics_GuildCountSlow()
    {
        if (!_configData.Prometheus.Enable)
            return;

        if (_promCount_GuildCount == null)
            throw new InvalidOperationException("InitializeMetrics not called, _promCount_GuildCount is null");
        if (_promCount_GuildUserCount == null)
            throw new InvalidOperationException("InitializeMetrics not called, _promCount_GuildUserCount is null");

        var taskList = new List<Task>();
        foreach (var guild in _client.Guilds)
        {
            taskList.Add(new Task(delegate
            {
                _promCount_GuildUserCount.WithLabels(
                    guild.Name ?? "<None>",
                    guild.Id.ToString()
                ).Set(guild.Users.Count);
            }));
        }
        await XeniaHelper.TaskWhenAll(taskList);
        _promCount_GuildCount.Set(_client.Guilds.Count);
    }
    #endregion

    #region Metrics (Triggered by Events)
    private Task Event_SlashCommandExecuted_Prom(SocketSlashCommand interaction)
    {
        if (!_configData.Prometheus.Enable)
            return Task.CompletedTask;
        if (_details.Platform != XeniaPlatform.Bot)
            return Task.CompletedTask;

        if (_promCount_InteractionCount == null)
            throw new InvalidOperationException("InitializeMetrics not called, _promCount_InteractionCount is null");
        SocketGuild? guild =
            interaction.GuildId == null ? null : _client.GetGuild(interaction.GuildId ?? 0);
        _promCount_InteractionCount?.WithLabels(
            guild?.Name ?? "<None>",
            (guild?.Id ?? 0).ToString(),
            $"{interaction.User.Username}#{interaction.User.Discriminator}",
            interaction.User.Id.ToString(),
            interaction.Channel.Id.ToString(),
            interaction.Channel.Name,
            interaction.Data.Name,
            interaction.Data.Id.ToString()
        ).Inc();
        return Task.CompletedTask;
    }
    #endregion
    #endregion

    #region Event Emit
    public event DiscordControllerDelegate? Ready;
    public bool IsReady { get; private set; }
    private void InvokeReady()
    {
        if (Ready != null && !IsReady)
        {
            IsReady = true;
            Ready?.Invoke(this);
        }
    }

    public event Func<SocketMessage, Task>? MessageReceived;
    #endregion

    #region Event Handling
    private async Task _client_Ready()
    {
        InvokeReady();
        if (_interactionHandler != null)
            await _interactionHandler.InitializeAsync();
        var versionString = "v0.0";
        if (_details.VersionRaw != null)
        {
            versionString = $"v{_details.VersionRaw.Major}.{_details.VersionRaw.Minor}";
        }

        if (_details.Platform == XeniaPlatform.Bot)
        {
            await _client.SetGameAsync($"{versionString} | xenia.kate.pet", null);
        }
    }

    private static Task DiscordClientLogHandler(LogMessage arg)
    {
        var discordLog = LogManager.GetLogger("Discord" + (string.IsNullOrEmpty(arg.Source) ? "" : "." + arg.Source));
        switch (arg.Severity)
        {
            case LogSeverity.Debug:
            case LogSeverity.Verbose:
                discordLog.Debug(arg.Exception, arg.Message);
                discordLog.Debug(arg.Exception, arg.Message);
                break;
            case LogSeverity.Info:
                discordLog.Info(arg.Exception, arg.Message);
                break;
            case LogSeverity.Warning:
                discordLog.Warn(arg.Exception, arg.Message);
                break;
            case LogSeverity.Error:
                discordLog.Error(arg.Exception, arg.Message);
                break;
            case LogSeverity.Critical:
                discordLog.Fatal(arg.Exception, arg.Message);
                break;
        }
        return Task.CompletedTask;
    }
    #endregion

    public static DiscordSocketConfig GetSocketClientConfig()
    {
        return new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.All,
            UseInteractionSnowflakeDate = false,
            AlwaysDownloadUsers = true
        };
    }
}
