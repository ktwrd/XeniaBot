using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Discord.WebSocket;
using Prometheus;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.Shared.Services;

public partial class DiscordService
{
    private Gauge? _promCount_GuildCount = null;
    private Gauge? _promCount_GuildUserCount = null;
    private Counter? _promCount_InteractionCount = null;
    private async Task InitializeMetrics()
    {
        if (!_configData.Prometheus.Enable)
        {
            Log.WriteLine("Ignoring, Prometheus Metrics are disabled");
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
        
        // Create timer
        CreateMetricTimer();

        _prom.ReloadMetrics += ReloadMetrics;
    }

    #region Metric Timer
    /// <summary>
    /// When to update metrics, measured in seconds.
    /// </summary>
    private const int MetricTimerInterval = 30;

    private Timer _metricTimer;
    private void CreateMetricTimer()
    {
        if (!_configData.Prometheus.Enable)
            return;
        
        _metricTimer = new Timer(MetricTimerInterval);
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
            throw new Exception("InitializeMetrics not called, _promCount_GuildCount is null");
        if (_promCount_GuildUserCount == null)
            throw new Exception("InitializeMetrics not called, _promCount_GuildUserCount is null");
        
        var taskList = new List<Task>();
        foreach (var guild in _client.Guilds)
        {
            taskList.Add(new Task(delegate
            {
                _promCount_GuildUserCount.WithLabels(new string[]
                {
                    guild.Name ?? "<None>",
                    guild.Id.ToString()
                }).Set(guild.Users.Count);
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
        if (_details.Platform == XeniaPlatform.WebPanel)
            return Task.CompletedTask;

        if (_promCount_InteractionCount == null)
            throw new Exception("InitializeMetrics not called, _promCount_InteractionCount is null");
        SocketGuild? guild =
            interaction.GuildId == null ? null : _client.GetGuild(interaction.GuildId ?? 0);
        _promCount_InteractionCount?.WithLabels(new string[]
        {
            guild?.Name ?? "<None>",
            (guild?.Id ?? 0).ToString(),
            $"{interaction.User.Username}#{interaction.User.Discriminator}",
            interaction.User.Id.ToString(),
            interaction.Channel.Id.ToString(),
            interaction.Channel.Name,
            interaction.Data.Name,
            interaction.Data.Id.ToString()
        }).Inc();
        return Task.CompletedTask;
    }
    #endregion
}