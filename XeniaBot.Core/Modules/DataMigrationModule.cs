using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using XeniaBot.MongoData.Repositories;
using XeniaBot.Shared;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models;
using XeniaDiscord.Data.Models.BanSync;

namespace XeniaBot.Core.Modules;

[Group("data-migration", "Used for the MongoDB -> PostgreSQL data migration.")]
public class DataMigrationModule : InteractionModuleBase
{
    private readonly ConfigData _config;
    private readonly XeniaDbContext _db;
    private readonly BanSyncConfigRepository _mongoBanSyncConfigRepository;
    private readonly BanSyncStateHistoryRepository _mongoBanSyncStateHistoryRepository;
    private readonly BanSyncInfoRepository _mongoBanSyncInfoRepository;
    public DataMigrationModule(IServiceProvider services)
    {
        _config = services.GetRequiredService<ConfigData>();
        _db = services.GetRequiredService<XeniaDbContext>();

        _mongoBanSyncConfigRepository = services.GetRequiredService<BanSyncConfigRepository>();
        _mongoBanSyncStateHistoryRepository = services.GetRequiredService<BanSyncStateHistoryRepository>();
        _mongoBanSyncInfoRepository = services.GetRequiredService<BanSyncInfoRepository>();
    }

    [SlashCommand("bansync", "Migrate all BanSync-related tables")]
    public async Task BanSync()
    {
        if (!_config.UserWhitelist.Contains(Context.User.Id))
        {
            await Context.Interaction.RespondAsync("Invalid permissions.");
            return;
        }
        await Context.Interaction.RespondAsync("Started processing. You'll get updates about anything.");
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            var mongoGuildConfigCount = (await _mongoBanSyncConfigRepository.Count()).ToString("n0");
            await SendStatusUpdate($"Pulling MongoDB Records: Guild Config ({mongoGuildConfigCount})");
            var mongoGuildConfig = await _mongoBanSyncConfigRepository.GetAll();
            
            var mongoGuildStateCount = (await _mongoBanSyncStateHistoryRepository.Count()).ToString("n0");
            await SendStatusUpdate($"Pulling MongoDB Records: Guild State ({mongoGuildStateCount})");
            var mongoGuildState = await _mongoBanSyncStateHistoryRepository.GetAll();

            var mongoInfoCount = (await _mongoBanSyncInfoRepository.Count()).ToString("n0");
            await SendStatusUpdate($"Pulling MongoDB Records: Info ({mongoInfoCount})");
            var mongoInfo = await _mongoBanSyncInfoRepository.GetAll();

            await SendStatusUpdate("Mapping Guild Config");
            var guildConfig = mongoGuildConfig.DistinctBy(e => e.GuildId)
                .Where(e => e.GuildId > 0)
                .Select(m => new BanSyncGuildModel
                {
                    GuildId = m.GuildId.ToString(),
                    LogChannelId = m.LogChannel == 0 ? null : m.LogChannel.ToString(),
                    Enable = m.Enable,
                    State = m.State switch
                    {
                        MongoData.Models.BanSyncGuildState.Unknown => BanSyncGuildState.Unknown,
                        MongoData.Models.BanSyncGuildState.PendingRequest => BanSyncGuildState.PendingRequest,
                        MongoData.Models.BanSyncGuildState.RequestDenied => BanSyncGuildState.RequestDenied,
                        MongoData.Models.BanSyncGuildState.Blacklisted => BanSyncGuildState.Blacklisted,
                        MongoData.Models.BanSyncGuildState.Active => BanSyncGuildState.Active,
                        _ => BanSyncGuildState.Unknown
                    },
                    Notes = string.IsNullOrEmpty(m.Reason?.Trim()) ? null : m.Reason
                }).ToArray();

            await SendStatusUpdate("Mapping Guild State to Guild Snapshot");
            var guildSnapshots = mongoGuildState
                .OrderByDescending(e => e.Timestamp)
                .DistinctBy(e => new { e.GuildId, e.Timestamp })
                .Select(m => new BanSyncGuildSnapshotModel
                {
                    Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(m.Timestamp).UtcDateTime,
                    GuildId = m.GuildId.ToString(),
                    LogChannelId = null,
                    Enable = m.Enable,
                    State = m.State switch
                    {
                        MongoData.Models.BanSyncGuildState.Unknown => BanSyncGuildState.Unknown,
                        MongoData.Models.BanSyncGuildState.PendingRequest => BanSyncGuildState.PendingRequest,
                        MongoData.Models.BanSyncGuildState.RequestDenied => BanSyncGuildState.RequestDenied,
                        MongoData.Models.BanSyncGuildState.Blacklisted => BanSyncGuildState.Blacklisted,
                        MongoData.Models.BanSyncGuildState.Active => BanSyncGuildState.Active,
                        _ => BanSyncGuildState.Unknown
                    },
                    Notes = m.Reason,
                })
                .ToArray();

            await SendStatusUpdate($"Mapping BanSync Info Records ({mongoInfoCount})");
            var bansyncRecords = new List<BanSyncRecordModel>(mongoInfo.Count);
            var userPartialSnapshots = new List<UserPartialSnapshotModel>(mongoInfo.Count);
            foreach (var m in mongoInfo.OrderByDescending(e => e.Timestamp))
            {
                var userSnapshot = new UserPartialSnapshotModel()
                {
                    UserId = m.UserId.ToString(),
                    Username = string.IsNullOrEmpty(m.UserName?.Trim()) ? "" : m.UserName.Trim(),
                    Discriminator = m.UserDiscriminator,
                    DisplayName = string.IsNullOrEmpty(m.UserDisplayName?.Trim()) ? "" : m.UserDisplayName.Trim()
                };
                var r = new BanSyncRecordModel
                {
                    GuildId = m.GuildId.ToString(),
                    GuildName = string.IsNullOrEmpty(m.GuildName?.Trim()) ? "" : m.GuildName.Trim(),
                    UserId = m.UserId.ToString(),
                    UserPartialSnapshotId = userSnapshot.Id,
                    CreatedAt = DateTimeOffset.FromUnixTimeSeconds(m.Timestamp).UtcDateTime,
                    Reason = string.IsNullOrEmpty(m.Reason?.Trim()) ? null : m.Reason?.Trim(),
                    Ghost = m.Ghost,
                    UserPartialSnapshot = userSnapshot,
                    Source = BanSyncRecordSource.DataMigration_MongoDb
                };
                if (string.Equals("null", r.Reason, StringComparison.OrdinalIgnoreCase)
                    || string.Equals("<null>", r.Reason, StringComparison.OrdinalIgnoreCase)
                    || string.Equals("<unknown>", r.Reason, StringComparison.OrdinalIgnoreCase)
                    || string.Equals("No reason given.", r.Reason, StringComparison.OrdinalIgnoreCase))
                {
                    r.Reason = null;
                }
                userSnapshot.CreatedAt = r.CreatedAt <= DateTime.UnixEpoch ? DateTime.UtcNow : r.CreatedAt;

                // try to persist RecordId if we can
                if (Guid.TryParse(m.RecordId, out var parsedRecordId) &&
                    !bansyncRecords.Any(e => e.Id == parsedRecordId))
                {
                    r.Id = parsedRecordId;
                }
                bansyncRecords.Add(r);
                userPartialSnapshots.Add(userSnapshot);
            }

            await SendStatusUpdate("Inserting Guild Config: " + guildConfig.Length.ToString("n0"));
            await db.BanSyncGuilds.AddRangeAsync(guildConfig);
            await SendStatusUpdate("Inserting Guild Snapshots: " + guildSnapshots.Length.ToString("n0"));
            await db.BanSyncGuildSnapshots.AddRangeAsync(guildSnapshots);
            await SendStatusUpdate("Inserting User Partial Snapshots: " + userPartialSnapshots.Count.ToString("n0"));
            await db.UserPartialSnapshots.AddRangeAsync(userPartialSnapshots);
            await SendStatusUpdate("Inserting BanSync Records: " + bansyncRecords.Count.ToString("n0"));
            await db.BanSyncRecords.AddRangeAsync(bansyncRecords);
            await SendStatusUpdate("Saving Changes");
            await db.SaveChangesAsync();
            await SendStatusUpdate("Commiting transaction");
            await trans.CommitAsync();
            await SendStatusUpdate("Complete!");
        }
        catch (Exception ex)
        {
            await Context.Channel.SendFileAsync(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ex.ToString())),
                "exception.txt",
                "Failed to migrate data!");
            await trans.RollbackAsync();
        }
    }
    
    private async Task SendStatusUpdate(
        string message)
    {
        var title = new List<string>();
        if (Context.Interaction.Data is IApplicationCommandInteractionData data)
        {
            title.Add(data.Name);
            foreach (var opt in data.Options)
            {
                title.Add(opt.Name);
            }
        }
        await Context.Channel.SendMessageAsync(
            embed: new EmbedBuilder()
            .WithTitle(string.Join("/", title))
            .WithDescription(message)
            .Build());
    }
}
