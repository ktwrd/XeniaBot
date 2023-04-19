using Discord;
using Discord.WebSocket;
using kate.shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using SkidBot.Core.Controllers.BotAdditions;
using SkidBot.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using SkidBot.Shared;
using Prometheus;

namespace SkidBot.Core.Controllers
{
    public class DiscordController
    {
        private readonly IServiceProvider _services;
        private readonly DiscordSocketClient _client;
        private readonly SkidConfig _config;
        private readonly CommandHandler _commandHandler;
        private readonly InteractionHandler _interactionHandler;
        private readonly PrometheusController _prom;
        public DiscordController(IServiceProvider services)
        {
            _config = services.GetRequiredService<SkidConfig>();
            _client = services.GetRequiredService<DiscordSocketClient>();

            _commandHandler = services.GetRequiredService<CommandHandler>();
            _interactionHandler = services.GetRequiredService<InteractionHandler>();

            _prom = services.GetRequiredService<PrometheusController>();

            _services = services;

            // Prometheus Events
            _prom.ServerStart += InitializeMetrics;
            _client.SlashCommandExecuted += Event_SlashCommandExecuted_Prom;

            _client.Log += _client_Log;
            _client.Ready += _client_Ready;
        }

        public async Task Run()
        {
            _client.MessageReceived += (arg) =>
            {
                MessageReceived?.Invoke(arg);
                return Task.CompletedTask;
            };

            await _client.LoginAsync(TokenType.Bot, _config.DiscordToken);
            await _client.StartAsync();
        }

        #region Prometheus Metrics
        private Gauge? _promCount_GuildCount = null;
        private Gauge? _promCount_GuildUserCount = null;
        private Counter? _promCount_InteractionCount = null;
        private async Task InitializeMetrics()
        {
            if (!_config.Prometheus_Enable)
            {
                Log.WriteLine("Ignoring, Prometheus Metrics are disabled");
                return;
            }
            _promCount_GuildCount = _prom.CreateGauge(
                "skid_discord_guild_count",
                "Amount of guilds this bot is in",
                publish: false);
            _promCount_GuildUserCount = _prom.CreateGauge(
                "skid_discord_guild_users", 
                "Amount of users per guild",
                labelNames: new string[]
                {
                    "guild_name",
                    "guild_id"
                },
                publish: false);
            _promCount_InteractionCount = _prom.CreateCounter(
                "skid_discord_interaction_count",
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
            if (!_config.Prometheus_Enable)
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
            if (!_config.Prometheus_Enable)
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
            if (!_config.Prometheus_Enable)
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
            await SGeneralHelper.TaskWhenAll(taskList);
            _promCount_GuildCount.Set(_client.Guilds.Count);
        }
        #endregion

        #region Metrics (Triggered by Events)
        private Task Event_SlashCommandExecuted_Prom(SocketSlashCommand interaction)
        {
            if (!_config.Prometheus_Enable)
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

        public event Func<SocketMessage, Task> MessageReceived;
        #endregion

        #region Event Handling
        private async Task _client_Ready()
        {
            InvokeReady();
            await _commandHandler.InitializeAsync();
            await _interactionHandler.InitializeAsync();
        }

        private Task _client_Log(LogMessage arg)
        {
            var methodName = arg.Source;
            var fileName = "Discord";
            if (methodName == "Discord")
                methodName = "";
            switch (arg.Severity)
            {
                case Discord.LogSeverity.Debug:
                    Log.Debug(arg.Message, methodname: methodName, methodfile: fileName);
                    break;
                case Discord.LogSeverity.Verbose:
                    Log.Debug(arg.Message, methodname: methodName, methodfile: fileName);
                    break;
                case Discord.LogSeverity.Info:
                    Log.WriteLine(arg.Message, methodname: methodName, methodfile: fileName);
                    break;
                case Discord.LogSeverity.Warning:
                    Log.Warn(arg.Message, methodname: methodName, methodfile: fileName);
                    break;
                case Discord.LogSeverity.Error:
                    Log.Error(arg.Message, methodname: methodName, methodfile: fileName);
                    break;
                case Discord.LogSeverity.Critical:
                    Log.Error(arg.Message, methodname: methodName, methodfile: fileName);
                    break;
            }
            if (arg.Exception != null)
                Log.Error(arg.Exception, methodname: methodName, methodfile: fileName);
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
}
