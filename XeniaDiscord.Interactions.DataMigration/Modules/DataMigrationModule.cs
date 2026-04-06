using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.Text.Json;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using XeniaBot.MongoData.Repositories;
using XeniaBot.Shared;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.BanSync;
using XeniaDiscord.Data.Models.Cache;
using XeniaDiscord.Data.Models.PartialSnapshot;
using XeniaDiscord.Data.Models.ServerLog;
using MongoBanSyncGuildState = XeniaBot.MongoData.Models.BanSyncGuildState;
using MongoServerLogRepository = XeniaBot.MongoData.Repositories.ServerLogRepository;

namespace XeniaDiscord.Interactions.DataMigration.Modules;

[Group("data-migration", "Used for the MongoDB -> PostgreSQL data migration.")]
[DeveloperModule]
[CommandContextType(InteractionContextType.Guild)]
public class DataMigrationModule : InteractionModuleBase
{
    private readonly ConfigData _config;
    private readonly XeniaDbContext _db;
    private readonly DiscordSocketClient _discord;
    private readonly BanSyncConfigRepository _mongoBanSyncConfigRepository;
    private readonly BanSyncStateHistoryRepository _mongoBanSyncStateHistoryRepository;
    private readonly BanSyncInfoRepository _mongoBanSyncInfoRepository;
    private readonly MongoServerLogRepository _mongoServerLogRepository;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public DataMigrationModule(IServiceProvider services)
    {
        _config = services.GetRequiredService<ConfigData>();
        _db = services.GetRequiredService<XeniaDbContext>();
        _discord = services.GetRequiredService<DiscordSocketClient>();

        _mongoBanSyncConfigRepository = services.GetRequiredService<BanSyncConfigRepository>();
        _mongoBanSyncStateHistoryRepository = services.GetRequiredService<BanSyncStateHistoryRepository>();
        _mongoBanSyncInfoRepository = services.GetRequiredService<BanSyncInfoRepository>();
        _mongoServerLogRepository = services.GetRequiredService<MongoServerLogRepository>();
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
            var mongoGuildStateCount = (await _mongoBanSyncStateHistoryRepository.Count()).ToString("n0");
            var mongoInfoCount = (await _mongoBanSyncInfoRepository.Count()).ToString("n0");

            await SendStatusUpdate($"Pulling MongoDB Records: Guild Config ({mongoGuildConfigCount})", StatusUpdateType.Downloading);
            var mongoGuildConfig = await _mongoBanSyncConfigRepository.GetAll();
            
            await SendStatusUpdate($"Pulling MongoDB Records: Guild State ({mongoGuildStateCount})", StatusUpdateType.Downloading);
            var mongoGuildState = await _mongoBanSyncStateHistoryRepository.GetAll();

            await SendStatusUpdate($"Pulling MongoDB Records: Info ({mongoInfoCount})", StatusUpdateType.Downloading);
            var mongoInfo = await _mongoBanSyncInfoRepository.GetAll();

            await SendStatusUpdate("Mapping Guild Config", StatusUpdateType.Mapping);
            var guildConfig = mongoGuildConfig.DistinctBy(e => e.GuildId)
                .Where(e => e.GuildId > 0)
                .Select(m => new BanSyncGuildModel
                {
                    GuildId = m.GuildId.ToString(),
                    LogChannelId = m.LogChannel == 0 ? null : m.LogChannel.ToString(),
                    Enable = m.Enable,
                    State = MapState(m.State),
                    Notes = string.IsNullOrEmpty(m.Reason?.Trim()) ? null : m.Reason
                }).ToArray();

            await SendStatusUpdate("Mapping Guild State to Guild Snapshot", StatusUpdateType.Mapping);
            var guildSnapshots = mongoGuildState
                .OrderByDescending(e => e.Timestamp)
                .DistinctBy(e => new { e.GuildId, e.Timestamp })
                .Select(m => new BanSyncGuildSnapshotModel
                {
                    Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(m.Timestamp).UtcDateTime,
                    GuildId = m.GuildId.ToString(),
                    LogChannelId = null,
                    Enable = m.Enable,
                    State = MapState(m.State),
                    Notes = m.Reason,
                })
                .ToArray();

            var weirdGuildModels = new List<BanSyncGuildModel>();

            await SendStatusUpdate($"Mapping BanSync Info Records ({mongoInfoCount})", StatusUpdateType.Mapping);
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
                    Reason = MapReason(m.Reason),
                    Ghost = m.Ghost,
                    UserPartialSnapshot = userSnapshot,
                    Source = BanSyncRecordSource.DataMigration_MongoDb
                };
                userSnapshot.Timestamp = r.CreatedAt <= DateTime.UnixEpoch ? DateTime.UtcNow : r.CreatedAt;

                if (!guildSnapshots.Any(e => e.GuildId == r.GuildId)
                    && !weirdGuildModels.Any(e => e.GuildId == r.GuildId))
                {
                    weirdGuildModels.Add(new()
                    {
                        GuildId = r.GuildId,
                        Notes = "From Data Migration: Weird guild, not referenced in MongoDB guilds collection."
                    });
                }

                // try to persist RecordId if we can
                if (Guid.TryParse(m.RecordId, out var parsedRecordId) &&
                    !bansyncRecords.Any(e => e.Id == parsedRecordId))
                {
                    r.Id = parsedRecordId;
                }
                bansyncRecords.Add(r);
                userPartialSnapshots.Add(userSnapshot);
            }

            await SendStatusUpdate("Inserting Guild Config: " + guildConfig.Length.ToString("n0"), StatusUpdateType.Inserting);
            await db.BanSyncGuilds.AddRangeAsync(guildConfig);
            await db.BanSyncGuilds.AddRangeAsync(weirdGuildModels);
            await SendStatusUpdate("Inserting Guild Snapshots: " + guildSnapshots.Length.ToString("n0"), StatusUpdateType.Inserting);
            await db.BanSyncGuildSnapshots.AddRangeAsync(guildSnapshots);
            await SendStatusUpdate("Inserting User Partial Snapshots: " + userPartialSnapshots.Count.ToString("n0"), StatusUpdateType.Inserting);
            await db.UserPartialSnapshots.AddRangeAsync(userPartialSnapshots);
            await SendStatusUpdate("Inserting BanSync Records: " + bansyncRecords.Count.ToString("n0"), StatusUpdateType.Inserting);
            await db.BanSyncRecords.AddRangeAsync(bansyncRecords);
            await SendStatusUpdate("Saving Changes", StatusUpdateType.Commiting);
            await db.SaveChangesAsync();
            await SendStatusUpdate("Commiting transaction", StatusUpdateType.Commiting);
            await trans.CommitAsync();
            await SendStatusUpdate("Complete!", StatusUpdateType.Done);
            if (weirdGuildModels.Count > 0)
            {
                var json = JsonSerializer.Serialize(weirdGuildModels, new JsonSerializerOptions()
                {
                    IncludeFields = true,
                    IgnoreReadOnlyFields = false,
                    IgnoreReadOnlyProperties = false,
                    WriteIndented = true
                });
                var tmpFilename = Path.GetTempFileName();
                await File.WriteAllTextAsync(tmpFilename, json);
                _log.Info($"Wrote weird guilds to: {tmpFilename}");
                await Context.Channel.SendFileAsync(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), "weird-guilds.json", "There were some weird guilds while doing the migration. Here's the records so any issues can be looked into.");
            }
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
    
    [SlashCommand("srvlog-cfg", "Configuration for Server Logging")]
    public async Task ServerLogConfig()
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
            var mongoDataCount = (await _mongoServerLogRepository.Count()).ToString("n0");

            await SendStatusUpdate($"Pulling MongoDB Records ({mongoDataCount})", StatusUpdateType.Downloading);
            var mongoData = await _mongoServerLogRepository.GetAll();
            
            await SendStatusUpdate("Mapping Models", StatusUpdateType.Mapping);
            var channelModels = new List<ServerLogChannelModel>();
            var guildModels = new List<ServerLogGuildModel>();
            var existingGuildIds = await db.GuildCache.AsNoTracking().Select(e => e.Id).ToListAsync();
            var missingGuildIds = mongoData.Select(e => e.ServerId.ToString()).Distinct().Where(e => !existingGuildIds.Contains(e)).ToList();
            if (missingGuildIds.Count > 0)
            {
                await SendStatusUpdate($"Mapping Models: Guild Cache ({missingGuildIds.Count})", StatusUpdateType.Mapping);
            }
            
            var guildCache = new List<GuildCacheModel>();
            foreach (var guildIdStr in missingGuildIds)
            {
                var guildId = guildIdStr.ParseRequiredULong(nameof(guildIdStr), false);
                var guild = _discord.GetGuild(guildId);
                if (guild == null)
                {
                    guildCache.Add(new GuildCacheModel(guildId)
                    {
                        CreatedAt = SnowflakeUtils.FromSnowflake(guildId).UtcDateTime
                    });
                }
                else
                {
                    guildCache.Add(new GuildCacheModel(guildId)
                    {
                        Name = guild.Name,
                        OwnerUserId = guild.OwnerId.ToString(),
                        CreatedAt = SnowflakeUtils.FromSnowflake(guildId).UtcDateTime,
                        IconUrl = guild.IconUrl,
                        BannerUrl = guild.BannerUrl,
                        SplashUrl = guild.SplashUrl,
                        DiscoverySplashUrl = guild.DiscoverySplashUrl
                    });
                }
            }

            mongoData.Reverse();
            foreach (var mongoModel in mongoData.DistinctBy(e => e.ServerId))
            {
                var model = new ServerLogGuildModel()
                {
                    GuildId = mongoModel.ServerId.ToString(),
                    Enabled = true
                };
                guildModels.Add(model);
                if (mongoModel.DefaultLogChannel > 0)
                {
                    channelModels.Add(new ServerLogChannelModel()
                    {
                        GuildId = model.GuildId,
                        ChannelId = mongoModel.DefaultLogChannel.ToString(),
                        Event = ServerLogEvent.Fallback,
                    });
                }
                AddChannel(mongoModel.MemberJoinChannel, ServerLogEvent.MemberJoin);
                AddChannel(mongoModel.MemberLeaveChannel, ServerLogEvent.MemberLeave);
                AddChannel(mongoModel.MemberBanChannel, ServerLogEvent.MemberBan);
                AddChannel(mongoModel.MemberKickChannel, ServerLogEvent.MemberKick);
                AddChannel(mongoModel.MessageEditChannel, ServerLogEvent.MessageEdit);
                AddChannel(mongoModel.MessageDeleteChannel, ServerLogEvent.MessageDelete);
                AddChannel(mongoModel.ChannelCreateChannel, ServerLogEvent.ChannelCreate);
                AddChannel(mongoModel.ChannelEditChannel, ServerLogEvent.ChannelEdit);
                AddChannel(mongoModel.ChannelDeleteChannel, ServerLogEvent.ChannelDelete);
                AddChannel(mongoModel.MemberVoiceChangeChannel, ServerLogEvent.MemberVoiceChange);
                
                void AddChannel(ulong? channelId, ServerLogEvent mappedEvent)
                {
                    if (channelId.HasValue && channelId.Value > 0)
                    {
                        channelModels.Add(new ServerLogChannelModel()
                        {
                            GuildId = model.GuildId,
                            ChannelId = channelId.Value.ToString(),
                            Event = mappedEvent
                        });
                    }
                }
            }
            
            if (guildCache.Count > 0)
            {
                await SendStatusUpdate("Inserting Guild Cache: " + guildCache.Count.ToString("n0"), StatusUpdateType.Inserting);
                await db.GuildCache.AddRangeAsync(guildCache);
            }
            
            await SendStatusUpdate("Inserting Server Log Guilds: " + guildModels.Count.ToString("n0"), StatusUpdateType.Inserting);
            await db.ServerLogGuilds.AddRangeAsync(guildModels);
            
            await SendStatusUpdate("Inserting Server Log Channels: " + channelModels.Count.ToString("n0"), StatusUpdateType.Inserting);
            await db.ServerLogChannels.AddRangeAsync(channelModels);
            
            await SendStatusUpdate("Saving Changes", StatusUpdateType.Commiting);
            await db.SaveChangesAsync();
            await SendStatusUpdate("Commiting transaction", StatusUpdateType.Commiting);
            await trans.CommitAsync();
            await SendStatusUpdate("Complete!", StatusUpdateType.Done);
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

    // TODO create command for "role preservation" module and include option to generate all snapshots for all guilds (like in DiscordCacheAdminModule)
    
    private async Task SendStatusUpdate(
        string message,
        StatusUpdateType status)
    {
        var title = new List<string>();
        if (Context.Interaction.Data is IApplicationCommandInteractionData data)
        {
            title.Add(data.Name);
            title.AddRange(data.Options.Select(e => e.Name));
        }
        var content = status switch
        {
            StatusUpdateType.Downloading => "📥",
            StatusUpdateType.Mapping => "🔀",
            StatusUpdateType.Inserting => "➕",
            StatusUpdateType.Commiting => "💾",
            StatusUpdateType.Done => "✔️"
        } + " " + message;
        await Context.Channel.SendMessageAsync(
            embed: new EmbedBuilder()
            .WithTitle(string.Join("/", title))
            .WithDescription(content)
            .Build());
    }
    private enum StatusUpdateType
    {
        Downloading,
        Mapping,
        Inserting,
        Commiting,
        Done
    }

    private static BanSyncGuildState MapState(MongoBanSyncGuildState state)
    {
        return state switch
        {
            MongoBanSyncGuildState.Unknown => BanSyncGuildState.Unknown,
            MongoBanSyncGuildState.PendingRequest => BanSyncGuildState.PendingRequest,
            MongoBanSyncGuildState.RequestDenied => BanSyncGuildState.RequestDenied,
            MongoBanSyncGuildState.Blacklisted => BanSyncGuildState.Blacklisted,
            MongoBanSyncGuildState.Active => BanSyncGuildState.Active,
            _ => BanSyncGuildState.Unknown
        };
    }

    private static string? MapReason(string? reason)
    {
        reason = reason?.Trim();
        if (reason == null
            || string.IsNullOrEmpty(reason)) return null;
        if (string.Equals("<null>", reason, StringComparison.OrdinalIgnoreCase)
            || string.Equals("<unknown>", reason, StringComparison.OrdinalIgnoreCase)
            || string.Equals("null", reason, StringComparison.OrdinalIgnoreCase)
            || string.Equals("No reason given", reason, StringComparison.OrdinalIgnoreCase))
            return null;
        return reason;
    }
}
