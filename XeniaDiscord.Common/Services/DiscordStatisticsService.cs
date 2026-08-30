using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Prometheus;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using XeniaDiscord.Data;

namespace XeniaDiscord.Common.Services;

[XeniaController]
public partial class DiscordStatisticsService : BaseService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly DiscordShardedClient? _client;
    private readonly ConfigData _configData;
    private readonly PrometheusService _prom;
    private readonly ProgramDetails _details;
    private readonly IDbContextFactory<XeniaDbContext> _dbContextFactory;
    public void Shutdown()
    {
        _prom.ServerStart -= InitializePrometheus;
        _prom.ReloadMetrics -= ReloadMetrics;
        ShutdownIncreaseEvents();
    }
    public DiscordStatisticsService(IServiceProvider services) : base(services)
    {
        _details = services.GetRequiredService<ProgramDetails>();

        _configData = services.GetRequiredService<ConfigData>();
        _client = services.GetRequiredService<DiscordShardedClient>();

        _prom = services.GetRequiredService<PrometheusService>();
        _dbContextFactory = services.GetRequiredService<IDbContextFactory<XeniaDbContext>>();

        _statGuilds = _prom.CreateGauge(
            "xenia_discord_guild_count",
            "Amount of guilds this bot is in",
            labelNames: [],
            publish: false);
        _statGuildMemberCount = _prom.CreateGauge(
            "xenia_discord_guild_users",
            "Amount of users per guild",
            labelNames: [
                "guild_name",
                "guild_id",
            ],
            publish: false);
        _statGuildChannels = _prom.CreateGauge(
            "xenia_discord_guild_channel_count",
            "Amount of channels that Xenia is in (per guild)",
            labelNames: [
                "guild_name",
                "guild_id",
                "channel_type"
            ],
            publish: false);
        _statChannels = _prom.CreateGauge(
            "xenia_discord_channels",
            "Amount of channels that Xenia is in. Includes channels in guilds",
            labelNames: [
                "guild_name",
                "guild_id"
            ],
            publish: false);
        _statDiscordLatency = _prom.CreateGauge(
            "xenia_discord_latency",
            "Latency (ms) to Discord",
            labelNames: [
                "user_id",
                "username",
                "global_name",
                "connection_state",
                "shard_id",
            ],
            publish: false);
        _statDiscordShards = _prom.CreateGauge(
            "xenia_discord_shards",
            "Shards",
            publish: false);
        _statInteractions = _prom.CreateCounter(
            "xenia_discord_interaction_count",
            "Interactions received",
            labelNames: [
                "guild_name",
                "guild_id",
                "author_name",
                "author_id",
                "channel_name",
                "channel_id",
                "interaction_group",
                "interaction_name",
                "interaction_id",
            ],
            publish: false);
        _statMessages = _prom.CreateCounter(
            "xenia_discord_message_count",
            "Messages received",
            labelNames: [
                "guild_id",
                "guild_name",
                "channel_id",
                "channel_name",
                "author_id",
                "author_name"
            ],
            publish: false);
        _statDiscordEvents = _prom.CreateCounter(
             "xenia_discord_event_count",
             "Events processed",
             labelNames: [
                 "type"
             ],
             publish: false);
        _statBanSyncRecords = _prom.CreateGauge(
            "xenia_discord_bansync_records",
            "BanSync Records",
            labelNames: [
                "guild_id",
                "guild_name"
            ],
            publish: false);
        _statBanSyncGuilds = _prom.CreateGauge(
            "xenia_discord_bansync_guilds",
            "BanSync Guilds",
            labelNames: [
                "state"
            ],
            publish: false);
        _statBanSyncGuildSnapshots = _prom.CreateGauge(
            "xenia_discord_bansync_guilds",
            "BanSync Guild Snapshots",
            publish: false);

        // Prometheus Events
        _prom.ServerStart += InitializePrometheus;
        _prom.ReloadMetrics += ReloadMetrics;
        if (_details.Platform != XeniaPlatform.Bot) return;

        _client.MessageReceived += OnMessageReceived;
        _client.SlashCommandExecuted += OnSlashCommandExecuted;
        InitializeIncreaseEvents();
    }

    public override async Task InitializeAsync()
    {
        await InitializePrometheus();
    }

    private Task InitializePrometheus()
    {
        CreateCollectionThread();
        return Task.CompletedTask;
    }

    private readonly Gauge _statGuilds;
    private readonly Gauge _statGuildMemberCount;
    private readonly Gauge _statGuildChannels;
    private readonly Gauge _statChannels;
    private readonly Gauge _statDiscordLatency;
    private readonly Gauge _statDiscordShards;
    private readonly Counter _statInteractions;
    private readonly Counter _statMessages;
    private readonly Counter _statDiscordEvents;

    private readonly Gauge _statBanSyncRecords;
    private readonly Gauge _statBanSyncGuilds;
    private readonly Gauge _statBanSyncGuildSnapshots;
    
    public DateTimeOffset? ReceivedLastEventAt { get; private set; }

    private void IncreaseEvent(DiscordStatisticsEventType type)
    {
        try
        {
            ReceivedLastEventAt = DateTimeOffset.UtcNow;
            _statDiscordEvents.WithLabels(type.ToString()).Inc();
        }
        catch (Exception ex)
        {
            _log.Warn(ex, $"Failed to increase for type: {type}");
        }
    }
}