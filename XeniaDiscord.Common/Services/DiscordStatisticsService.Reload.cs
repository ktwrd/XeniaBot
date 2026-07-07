using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using XeniaBot.Shared.Helpers;
using XeniaDiscord.Data.Models.BanSync;

namespace XeniaDiscord.Common.Services;

partial class DiscordStatisticsService
{
    #region Collection Thread
    private bool _collectionThreadExists = false;
    private void CreateCollectionThread()
    {
        if (_collectionThreadExists) return;
        new Thread(() =>
        {
            _collectionThreadExists = true;
            _log.Info("Created thread");
            while (true)
            {
                try
                {
                    MetricCollectionThread().GetAwaiter().GetResult();
                    break;
                }
                catch (Exception ex)
                {
                    _log.Warn(ex, $"Failed to call {nameof(MetricCollectionThread)}");
                    Task.Delay(500).Wait();
                }
            }
            _collectionThreadExists = false;
        })
        {
            Name = $"Xenia.{nameof(DiscordStatisticsService)}.{nameof(MetricCollectionThread)}"
        }.Start();
    }
    private async Task MetricCollectionThread()
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
                i++;
                await Task.Delay(5_000);
            }
            else
            {
                await Task.Delay(60_000);
            }
            if (!_collectionThreadExists)
            {
                break;
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
            ReloadMetrics_Channels(),

            ReloadMetrics_BanSync()
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
    private async Task ReloadMetrics_BanSyncRecordsByGuild()
    {
        using var trans = SentryHelper.CreateTransaction();
        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            var recordByGuilds = await db.BanSyncRecords
                .AsNoTracking()
                .Include(e => e.BanSyncGuild)
                .Where(e => e.BanSyncGuild != null && e.BanSyncGuild.State == BanSyncGuildState.Active)
                .GroupBy(e => e.GuildId)
                .Select(e => new {
                    GuildId = e.Key,
                    Count = e.Count()
                })
                .ToListAsync();
            var guildIds = recordByGuilds.Select(e => e.GuildId).Distinct().ToList();
            var guildSnapshots = await db.GuildPartialSnapshots
                .AsNoTracking()
                .OrderByDescending(e => e.Timestamp)
                .Where(e => guildIds.Contains(e.GuildId))
                .Select(e => new { e.GuildId, e.Name })
                .ToListAsync();
            foreach (var group in recordByGuilds)
            {
                var name = _client.Guilds.FirstOrDefault(e => e.Id.ToString() == group.GuildId)?.Name
                    ?? guildSnapshots.FirstOrDefault(e => e.GuildId == group.GuildId)?.Name;
                _statBanSyncRecords.WithLabels(group.GuildId, name ?? group.GuildId).Set(group.Count);
            }
            trans.Finish();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to make metrics");
            trans.Finish(ex);
        }
    }

    private async Task ReloadMetrics_BanSyncGuildsByState()
    {
        var trans = SentryHelper.CreateTransaction();
        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            var guildsByState = await db.BanSyncGuilds
                .AsNoTracking()
                .GroupBy(e => e.State)
                .Select(e => new {
                    State = e.Key,
                    Count = e.Count()
                })
                .ToListAsync();
            foreach (var group in guildsByState)
            {
                _statBanSyncGuilds.WithLabels(group.State.ToString()).Set(group.Count);
            }
            trans.Finish();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to make metrics");
            trans.Finish(ex);
        }
    }
    private async Task ReloadMetrics_BanSyncGuildSnapshots()
    {
        var trans = SentryHelper.CreateTransaction();
        try
        {
            await using var db = _db.CreateSession();
            var guildSnapshotCount = await db.BanSyncGuildSnapshots.AsNoTracking().LongCountAsync();
            _statBanSyncGuildSnapshots.Set(guildSnapshotCount);
            trans.Finish();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to make metrics");
            trans.Finish(ex);
        }
    }
    private async Task ReloadMetrics_BanSync()
    {
        if (!_configData.Prometheus.Enable) return;

        await Task.WhenAll(
            ReloadMetrics_BanSyncRecordsByGuild(),
            ReloadMetrics_BanSyncGuildsByState(),
            ReloadMetrics_BanSyncGuildSnapshots());
    }
    private Task ReloadMetrics_Channels()
    {
        if (!_configData.Prometheus.Enable) return Task.CompletedTask;

        long count = 0;
        count += _client.GroupChannels.Count;
        count += _client.PrivateChannels.Count;
        count += _client.Guilds.Select(e => e.Channels.Count).Sum();

        _statChannels.Set(count);

        return Task.CompletedTask;
    }
    private async Task ReloadMetrics_GuildChannels()
    {
        if (!_configData.Prometheus.Enable) return;
        var trans = SentryHelper.CreateTransaction();
        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            foreach (var guild in _client.Guilds)
            {
                var guildIdStr = guild.Id.ToString();
                var name = await db.GuildPartialSnapshots.AsNoTracking()
                    .Where(e => e.GuildId == guildIdStr)
                    .OrderByDescending(e => e.Timestamp)
                    .Select(e => e.Name)
                    .FirstOrDefaultAsync();
                var guildName = guild.Name ?? name ?? guildIdStr;
                var groups = new (long Count, string Ident)[]
                {
                    (guild.TextChannels.Count, "text"),
                    (guild.StageChannels.Count, "stage"),
                    (guild.CategoryChannels.Count, "category"),
                    (guild.ThreadChannels.Count, "thread"),
                    (guild.ForumChannels.Count, "forum"),
                    (guild.MediaChannels.Count, "media"),
                    (guild.Channels.Count, "all"),
                    (guild.Channels.Count, "other"),
                };
                groups[^1].Count = guild.TextChannels.Select(e => e.Id)
                    .Concat(guild.StageChannels.Select(e => e.Id))
                    .Concat(guild.CategoryChannels.Select(e => e.Id))
                    .Concat(guild.ThreadChannels.Select(e => e.Id))
                    .Concat(guild.ForumChannels.Select(e => e.Id))
                    .Concat(guild.MediaChannels.Select(e => e.Id))
                    .Distinct()
                    .Count();
                foreach (var (count, ident) in groups)
                {
                    _statGuildChannels.WithLabels(
                        guildName,
                        guildIdStr,
                        ident).Set(count);
                }
            }
            trans.Finish();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to make metrics");
            trans.Finish(ex);
        }
    }

    /// <summary>
    /// Update <see cref="_statGuilds"/> and <see cref="_statGuildMemberCount"/>
    /// </summary>
    private Task ReloadMetrics_GuildCount()
    {
        if (!_configData.Prometheus.Enable) return Task.CompletedTask;

        var trans = SentryHelper.CreateTransaction();
        try
        {
            foreach (var guild in _client.Guilds)
            {
                _statGuildMemberCount.WithLabels(
                        guild.Name ?? guild.Id.ToString(),
                        guild.Id.ToString())
                    .Set(guild.Users.Count);
            }
            _statGuilds
                .WithLabels()
                .Set(_client.Guilds.Count);
            trans.Finish();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to make metrics");
            trans.Finish(ex);
        }
        return Task.CompletedTask;
    }

    private string[] _metricsLatencyStrings = [];
    private Task ReloadMetrics_Latency()
    {
        if (!_configData.Prometheus.Enable) return Task.CompletedTask;

        var usernameFormatted = "";
        var displayName = "";
        var userId = "";
        var latency = _client.Latency;
        if (_client.CurrentUser != null)
        {
            userId = _client.CurrentUser.Id.ToString();
            usernameFormatted = _client.CurrentUser.Username;
            if (!string.IsNullOrEmpty(_client.CurrentUser.Discriminator.Trim('0')))
                usernameFormatted += $"#{_client.CurrentUser.Discriminator}";
            displayName = usernameFormatted;
            if (!string.IsNullOrEmpty(_client.CurrentUser.GlobalName))
                displayName = _client.CurrentUser.GlobalName;
        }

        var connectionState = _client.ConnectionState.ToString();
        if (_metricsLatencyStrings.Length == 4 &&
            connectionState != _metricsLatencyStrings[3])
        {
            _statDiscordLatency.RemoveLabelled(_metricsLatencyStrings);
        }
        _metricsLatencyStrings = [
            userId,
            usernameFormatted,
            displayName,
            _client.ConnectionState.ToString()
        ];
        _statDiscordLatency.WithLabels(_metricsLatencyStrings).Set(latency);
        return Task.CompletedTask;
    }
    #endregion
}
