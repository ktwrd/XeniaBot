using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Prometheus;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using XeniaDiscord.Data;

namespace XeniaDiscord.Common.Services;

[XeniaController]
public class DiscordStatisticsService : BaseService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly DiscordSocketClient _client;
    private readonly ConfigData _configData;
    private readonly PrometheusService _prom;
    private readonly ProgramDetails _details;
    private readonly XeniaDbContext _db;
    public DiscordStatisticsService(IServiceProvider services) : base(services)
    {
        _details = services.GetRequiredService<ProgramDetails>();

        _configData = services.GetRequiredService<ConfigData>();
        _client = services.GetRequiredService<DiscordSocketClient>();

        _prom = services.GetRequiredService<PrometheusService>();
        _db = services.GetRequiredScopedService<XeniaDbContext>(out var scope);

        // Prometheus Events
        _prom.ServerStart += InitializePrometheus;
        _prom.ReloadMetrics += ReloadMetrics;
        if (_details.Platform == XeniaPlatform.Bot)
        {
            _client.MessageReceived += OnMessageReceived;
            _client.SlashCommandExecuted += OnSlashCommandExecuted;
        }
        
        _statGuilds = _prom.CreateGauge(
            "xenia_discord_guild_count",
            "Amount of guilds this bot is in",
            publish: false);
        _statGuildMemberCount = _prom.CreateGauge(
            "xenia_discord_guild_users",
            "Amount of users per guild",
            labelNames: [
                "guild_name",
                "guild_id"
            ],
            publish: false);
        _statGuildChannels = _prom.CreateGauge(
            "xenia_discord_guild_channel_count",
            "Amount of channels that Xenia is in (per guild)",
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
                "connection_state"
            ],
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
    private readonly Counter _statInteractions;
    private readonly Counter _statMessages;

    private readonly Gauge _statBanSyncRecords;
    private readonly Gauge _statBanSyncGuilds;
    private readonly Gauge _statBanSyncGuildSnapshots;

    #region Collection Thread
    private bool CollectionThreadExists = false;
    private void CreateCollectionThread()
    {
        if (CollectionThreadExists) return;
        new Thread(() =>
        {
            CollectionThreadExists = true;
            Log.Info("Created thread");
            while (true)
            {
                try
                {
                    MetricCollectionThreadCallback().GetAwaiter().GetResult();
                    break;
                }
                catch (Exception ex)
                {
                    Log.Warn(ex, $"Failed to call {nameof(MetricCollectionThreadCallback)}");
                    Task.Delay(500).Wait();
                }
            }
            CollectionThreadExists = false;
        })
        {
            Name = $"{nameof(DiscordStatisticsService)}.MetricCollectionThread"
        }.Start();
    }
    private async Task MetricCollectionThreadCallback()
    {
        int i = 1;
        while (true)
        {
            if (_configData.Prometheus.Enable)
            {
                await ReloadMetricsFrequent();

                if (i % 3 == 0)
                {
                    await ReloadMetricsSlow();
                    if (i > 1000)
                    {
                        i = 1;
                    }
                }
                await Task.Delay(5_000);
            }
            else
            {
                await Task.Delay(60_000);
            }
        }
    }
    #endregion

    #region Reload Metrics
    public async Task ReloadMetrics()
    {
        await Task.WhenAll(
            ReloadMetricsSlow(),
            ReloadMetricsFrequent()
        );
    }
    /// <summary>
    /// Called every 15s
    /// </summary>
    private async Task ReloadMetricsSlow()
    {
        if (!_configData.Prometheus.Enable) return;

        var taskList = new[]
        {
            ReloadMetrics_GuildCount(),
            ReloadMetrics_GuildChannels(),
            ReloadMetrics_Channels()
        };
        await Task.WhenAll(taskList);
    }
    /// <summary>
    /// Called every 5s
    /// </summary>
    private async Task ReloadMetricsFrequent()
    {
        if (!_configData.Prometheus.Enable) return;
        var tasks = new[]
        {
            ReloadMetrics_Latency()
        };
        await Task.WhenAll(tasks);
    }

    private async Task ReloadMetrics_Channels()
    {
        if (!_configData.Prometheus.Enable) return;

        long count = (await _client.GetGroupChannelsAsync()).Count();
        count += _client.PrivateChannels.Count;
        count += _client.Guilds.Select(e => e.Channels.Count).Sum();

        _statChannels.Set(count);
    }
    private async Task ReloadMetrics_GuildChannels()
    {
        if (!_configData.Prometheus.Enable) return;

        await Task.WhenAll(_client.Guilds.Select(UpdateGuild));

        async Task UpdateGuild(SocketGuild guild)
        {
            var guildIdStr = guild.Id.ToString();
            var name = await _db.GuildPartialSnapshots.AsNoTracking()
                .Where(e => e.GuildId == guildIdStr)
                .OrderByDescending(e => e.Timestamp)
                .Select(e => e.Name)
                .FirstOrDefaultAsync();
            var guildName = guild.Name ?? name ?? guildIdStr;

            _statGuildMemberCount.WithLabels(
                guildIdStr,
                guildName,
                "text"
            ).Set(guild.TextChannels.Count);
            _statGuildChannels.WithLabels(
                guildIdStr,
                guildName,
                "stage"
                ).Set(guild.StageChannels.Count);
            _statGuildChannels.WithLabels(
                guildIdStr,
                guildName,
                "category"
                ).Set(guild.CategoryChannels.Count);
            _statGuildChannels.WithLabels(
                guildIdStr,
                guildName,
                "thread"
                ).Set(guild.ThreadChannels.Count);
            _statGuildChannels.WithLabels(
                guildIdStr,
                guildName,
                "forum"
                ).Set(guild.ForumChannels.Count);
            _statGuildChannels.WithLabels(
                guildIdStr,
                guildName,
                "media"
                ).Set(guild.MediaChannels.Count);
            _statGuildChannels.WithLabels(
                guildIdStr,
                guildName,
                "all"
                ).Set(guild.Channels.Count);
            var otherCount = guild.Channels.Count
                - (guild.TextChannels.Count
                 + guild.StageChannels.Count
                 + guild.CategoryChannels.Count
                 + guild.ThreadChannels.Count
                 + guild.ForumChannels.Count
                 + guild.MediaChannels.Count);
            _statGuildChannels.WithLabels(
                guildIdStr,
                guildName,
                "other"
                ).Set(otherCount);
        }
    }
    
    /// <summary>
    /// Update <see cref="_statGuilds"/> and <see cref="_statGuildMemberCount"/>
    /// </summary>
    private async Task ReloadMetrics_GuildCount()
    {
        if (!_configData.Prometheus.Enable) return;

        await Task.WhenAll(_client.Guilds.Select(UpdateGuild));
        _statGuilds.Set(_client.Guilds.Count);

        async Task UpdateGuild(SocketGuild guild)
        {
            var guildIdStr = guild.Id.ToString();
            var name = await _db.GuildPartialSnapshots.AsNoTracking()
                .Where(e => e.GuildId == guildIdStr)
                .OrderByDescending(e => e.Timestamp)
                .Select(e => e.Name)
                .FirstOrDefaultAsync();
            _statGuildMemberCount.WithLabels(
                guild.Name ?? name ?? guildIdStr,
                guildIdStr
            ).Set(guild.Users.Count);
        }
    }
    private Task ReloadMetrics_Latency()
    {
        if (!_configData.Prometheus.Enable) return Task.CompletedTask;

        var usernameFormatted = _client.CurrentUser.Username;
        if (!string.IsNullOrEmpty(_client.CurrentUser.Discriminator.Trim('0')))
            usernameFormatted += $"#{_client.CurrentUser.Discriminator}";
        var displayName = usernameFormatted;
        if (!string.IsNullOrEmpty(_client.CurrentUser.GlobalName))
            displayName = _client.CurrentUser.GlobalName;

        _statDiscordLatency.WithLabels(
            _client.CurrentUser.Id.ToString(),
            usernameFormatted,
            displayName,
            _client.ConnectionState.ToString()
        ).Set(_client.Latency);
        return Task.CompletedTask;
    }
    #endregion

    private async Task OnSlashCommandExecuted(SocketSlashCommand interaction)
    {
        if (!_configData.Prometheus.Enable) return;
        if (_details.Platform != XeniaPlatform.Bot) return;

        SocketGuild? guild = interaction.GuildId.HasValue ? _client.GetGuild(interaction.GuildId.Value) : null;
        var usernameFormatted = interaction.User.Username;
        if (!string.IsNullOrEmpty(interaction.User.Discriminator?.Trim('0')))
            usernameFormatted += $"#{interaction.User.Discriminator}";

        var guildIdStr = interaction.GuildId?.ToString();
        string? guildName = guildIdStr;
        if (guildIdStr != null)
        {
            guildName = guild?.Name;
            if (guildName != null)
            {
                guildName = await _db.GuildPartialSnapshots.AsNoTracking()
                    .Where(e => e.GuildId == guildIdStr)
                    .OrderByDescending(e => e.Timestamp)
                    .Select(e => e.Name)
                    .FirstOrDefaultAsync();
            }
        }

        var channelIdStr = interaction.ChannelId?.ToString();
        string? channelName = null;
        if (channelIdStr != null && interaction.Channel != null)
        {
            channelName = interaction.Channel.Name;
        }

        var interactionName = interaction.Data.Name;
        var interactionGroup = string.Empty;

        if (interaction.Data is IApplicationCommandInteractionData data)
        {
            var any = false;
            foreach (var opt in data.Options)
            {
                any = true;
                interactionName += $" {opt.Name}";
            }
            if (any)
            {
                interactionGroup = data.Name;
            }
        }

        _statInteractions.WithLabels(
            guildName ?? "",
            guildIdStr ?? "",
            usernameFormatted,
            interaction.User.Id.ToString(),
            channelName ?? "",
            channelIdStr ?? "",
            interactionGroup,
            interactionName,
            interaction.Data.Id.ToString()
        ).Inc();

        await using var trans = await _db.Database.BeginTransactionAsync();
        try
        {
            string authorIdStr = interaction.User.Id.ToString();
            if (await _db.InteractionStatistics.AnyAsync(e
                => e.InteractionGroup == interactionGroup
                && e.InteractionName == interactionName
                && e.GuildId == guildIdStr
                && e.ChannelId == channelIdStr
                && e.UserId == authorIdStr))
            {
                if (guildIdStr == null && channelIdStr == null)
                {
                    await _db.Database.ExecuteSqlAsync(
                        $"UPDATE public.\"Statistics_Interactions\" SET \"Count\" = \"Count\" + 1 WHERE \"InteractionGroup\" = {interactionGroup} AND \"InteractionName\" = {interactionName} AND \"GuildId\" IS NULL AND \"ChannelId\" IS NULL AND \"UserId\" = {authorIdStr};");
                }
                else if (guildIdStr == null && channelIdStr != null)
                {
                    await _db.Database.ExecuteSqlAsync(
                        $"UPDATE public.\"Statistics_Interactions\" SET \"Count\" = \"Count\" + 1 WHERE \"InteractionGroup\" = {interactionGroup} AND \"InteractionName\" = {interactionName} AND \"GuildId\" = {guildIdStr} AND \"ChannelId\" IS NULL AND \"UserId\" = {authorIdStr};");
                }
                else if (guildIdStr != null && channelIdStr == null)
                {
                    await _db.Database.ExecuteSqlAsync(
                        $"UPDATE public.\"Statistics_Interactions\" SET \"Count\" = \"Count\" + 1 WHERE \"InteractionGroup\" = {interactionGroup} AND \"InteractionName\" = {interactionName} AND \"GuildId\" IS NULL AND \"ChannelId\" = {channelIdStr} AND \"UserId\" = {authorIdStr};");
                }
                else
                {
                    await _db.Database.ExecuteSqlAsync(
                        $"UPDATE public.\"Statistics_Interactions\" SET \"Count\" = \"Count\" + 1 WHERE \"InteractionGroup\" = {interactionGroup} AND \"InteractionName\" = {interactionName} AND \"GuildId\" = {guildIdStr} AND \"ChannelId\" = {channelIdStr} AND \"UserId\" = {authorIdStr};");
                }
            }
            else
            {
                await _db.InteractionStatistics.AddAsync(new Data.Models.InteractionStatisticModel()
                {
                    InteractionGroup = interactionGroup,
                    InteractionName = interactionName,
                    GuildId = guildIdStr,
                    ChannelId = channelIdStr,
                    UserId = authorIdStr,
                    Count = 1
                });
            }
            await _db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
        }
    }

    private Task OnMessageReceived(SocketMessage message)
    {
        var usernameFormatted = message.Author.Username;
        if (!string.IsNullOrEmpty(message.Author.Discriminator?.Trim('0')))
            usernameFormatted += $"#{message.Author.Discriminator}";
        var labels = new[]
        {
            string.Empty,                   // guild_id
            string.Empty,                   // guild_name
            string.Empty,                   // channel_id
            string.Empty,                   // channel_name
            message.Author.Id.ToString(),   // author_id
            usernameFormatted               // author_name
        };
        if (message.Channel is SocketGuildChannel guildChannel)
        {
            labels[0] = guildChannel.Guild.Id.ToString();
            labels[1] = guildChannel.Guild.Name;
        }
        if (message.Channel != null)
        {
            labels[2] = message.Channel.Id.ToString();
            labels[3] = message.Channel.Name;
        }

        _statMessages.WithLabels(labels).Inc();

        return Task.CompletedTask;
    }
}