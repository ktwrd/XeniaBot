using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using XeniaDiscord.Common.Exceptions;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.BanSync;
using XeniaDiscord.Data.Models.Cache;
using XeniaDiscord.Data.Models.PartialSnapshot;
using XeniaDiscord.Data.Repositories;

namespace XeniaDiscord.Common.Services;

public class BanSyncService : BaseService
{
    private readonly Logger _log = LogManager.GetLogger("Xenia." + nameof(BanSyncService));

    private readonly DiscordSocketClient _client;
    private readonly ConfigData _configData;
    private readonly ErrorReportService _err;
    private readonly BanSyncGuildRepository _bansyncGuildRepository;
    private readonly BanSyncRecordRepository _bansyncRecordsRepository;
    private readonly AuditLogEntryCacheRepository _auditLogEntryRepository;
    private readonly ProgramDetails _programDetails;
    private readonly XeniaDbContext _db;

    public BanSyncService(IServiceProvider services)
        : base(services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        _configData = services.GetRequiredService<ConfigData>();
        _err = services.GetRequiredService<ErrorReportService>();
        _db = services.GetRequiredScopedService<XeniaDbContext>(out var scope);
        _bansyncGuildRepository = (scope?.ServiceProvider ?? services).GetRequiredService<BanSyncGuildRepository>();
        _bansyncRecordsRepository = (scope?.ServiceProvider ?? services).GetRequiredService< BanSyncRecordRepository>();
        _auditLogEntryRepository = (scope?.ServiceProvider ?? services).GetRequiredService<AuditLogEntryCacheRepository>();

        _programDetails = services.GetRequiredService<ProgramDetails>();

        if (_programDetails.Platform != XeniaPlatform.WebPanel)
        {
            _client.UserJoined += DiscordClientOnUserJoined;
            _client.UserBanned += DiscordClientOnUserBanned;
        }
    }

    public override async Task OnReady()
    {
        if (_programDetails.Platform != XeniaPlatform.Bot)
        {
            _log.Info($"Skipping since not running on {XeniaPlatform.Bot} (platform: {_programDetails.Platform})");
            return;
        }
        if (!_configData.RefreshBansOnStart)
        {
            _log.Info($"Skipping ban refresh since it's disabled.");
            return;
        }

        foreach (var guild in _client.Guilds)
        {
            try
            {
                await RefreshBans(guild);
            }
            catch (Exception ex)
            {
                _log.Warn(ex, $"Failed to refresh bans for Guild \"{guild.Name}\" ({guild.Id})");
            }
        }
    }
    
    private static string? ParseReason(string? reason)
    {
        reason = reason?.Trim();
        if (string.IsNullOrEmpty(reason)
            || string.Equals("<null>", reason, StringComparison.OrdinalIgnoreCase)
            || string.Equals("<unknown>", reason, StringComparison.OrdinalIgnoreCase)
            || string.Equals("null", reason, StringComparison.OrdinalIgnoreCase)
            || string.Equals("No reason given", reason, StringComparison.OrdinalIgnoreCase))
            return null;
        else
            return reason.Trim();
    }

    public static bool InfoEquals(BanSyncRecordModel self, BanSyncRecordModel other)
    {
        return self.UserId == other.UserId
            && self.GuildId == other.GuildId
            && self.BannedByUserId == other.BannedByUserId
            && ParseReason(self.Reason) == ParseReason(other.Reason);
    }

    public static bool InfoEquals(BanSyncRecordModel self, RestBan other, ulong otherGuildId)
    {
        return self.GetUserId() == other.User.Id
            && self.GetGuildId() == otherGuildId
            && string.Equals(ParseReason(self.Reason), ParseReason(other.Reason), StringComparison.OrdinalIgnoreCase);
    }

    public Task RefreshBans(ulong guildId) => RefreshBans(_client.GetGuild(guildId));
    public async Task RefreshBans(SocketGuild guild, bool ignoreExisting = true)
    {
        var config = await _bansyncGuildRepository.GetAsync(guild.Id);
        if (config?.Enable != true || config.State != BanSyncGuildState.Active)
            return;

        const int pageSize = 1000;
        long total = 0;

        _log.Debug($"Fetching bans for guild \"{guild.Name}\" ({guild.Id})");
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            var bans = await guild.GetBansAsync(9223372036854775807, Direction.Before, pageSize).ToListAsync();
            while (true)
            {
                var bansArray = bans.SelectMany(e => e).ToArray();
                total += bansArray.Length;
                foreach (var ban in bansArray)
                {
                    await RefreshBans_ProcessBanCallback(db, ban, guild, ignoreExisting);
                }
                if (bansArray.Length < pageSize) break;

                bans = await guild.GetBansAsync(bansArray.Min(e => e.User.Id), Direction.Before, pageSize).ToListAsync();
            }
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            var msg = $"Failed to pull & insert bans for Guild \"{guild.Name}\" ({guild.Id})";
            _log.Error(ex, msg);
            throw new InvalidOperationException(msg, ex);
        }
        var totalStr = total.ToString("n0");
        _log.Debug($"Got {totalStr} records for guild \"{guild.Name}\" ({guild.Id})");

    }

    private async Task RefreshBans_ProcessBanCallback(
            XeniaDbContext db,
            RestBan ban,
            SocketGuild guild,
            bool ignoreExisting)
    {
        try
        {
            // only ignore when everything matches and ignoreExisting is true
            var existing = await _bansyncRecordsRepository.GetInfo(ban.User.Id, guild.Id, new()
            {
                IncludeGhostedRecords = true
            });
            if (ignoreExisting &&
                existing != null &&
                InfoEquals(existing, ban, guild.Id))
            {
                return;
            }
            var partialUserInfo = new UserPartialSnapshotModel()
            {
                UserId = ban.User.Id.ToString(),
                Username = ban.User.Username,
                Discriminator = ban.User.DiscriminatorValue == 0 ? null : ban.User.Discriminator,
                DisplayName = string.IsNullOrEmpty(ban.User.GlobalName) ? ban.User.Username : ban.User.GlobalName
            };
            var info = new BanSyncRecordModel()
            {
                UserId = ban.User.Id.ToString(),
                GuildId = guild.Id.ToString(),
                UserPartialSnapshotId = partialUserInfo.Id,
                GuildName = guild.Name,
                Reason = ParseReason(ban.Reason)
            };
            await db.UserPartialSnapshots.AddAsync(partialUserInfo);
            await _bansyncRecordsRepository.InsertOrUpdate(db, info);
        }
        catch (Exception ex)
        {
            var msg = $"Failed to add ban for {ban?.User.FormatUsername()} ({ban?.User.Id}) in guild {guild.Name} ({guild.Id})";
            _log.Error(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithGuild(guild)
                .AddSerializedAttachment("ban.json", ban));
        }
    }

    /// <summary>
    /// Add user to database and notify mutual servers. <see cref="NotifyBan(BanSyncInfoModel)"/>
    /// </summary>
    public async Task DiscordClientOnUserBanned(SocketUser user, SocketGuild guild)
    {
        try
        {
            await DiscordClientOnUserBannedInternal(user, guild);
        }
        catch (Exception ex)
        {
            var msg = $"Failed to process event UserBanned for {user.FormatUsername()} in Guild \"{guild.Name}\" (guildId={guild.Id}, userId={user.Id})";
            _log.Error(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithGuild(guild)
                .WithUser(user));
        }
    }

    private async Task DiscordClientOnUserBannedInternal(SocketUser user, SocketGuild guild)
    {
        var config = await _bansyncGuildRepository.GetAsync(guild.Id);
        if (config == null)
        {
            config = new BanSyncGuildModel(guild.Id)
            {
                Enable = false,
                State = BanSyncGuildState.Unknown
            };
            await _bansyncGuildRepository.InsertOrUpdate(_db, config);
            await _db.SaveChangesAsync();
        }

        RestBan? banInfo = null;
        try
        {
            banInfo = await guild.GetBanAsync(user);
        }
        catch (Exception ex)
        {
            var msg = $"Failed to get ban info for user {user} ({user.Id}) in guild \"{guild.Name}\" ({guild.Id})";
            if (config.Enable || config.State == BanSyncGuildState.Active)
            {
                await _err.Submit(new ErrorReportBuilder()
                    .WithException(ex)
                    .WithNotes(msg)
                    .WithUser(user)
                    .WithGuild(guild));
            }
            else
            {
                _log.Warn(ex, msg);
            }
        }
        RestAuditLogEntry? mostRelevantAuditLogEntry = null;
        try
        {
            mostRelevantAuditLogEntry = await TryGetRelevantAuditLogEntry();
            // wait 1s just to make sure that it's in the audit log
            if (mostRelevantAuditLogEntry == null)
            {
                await Task.Delay(1000);
                mostRelevantAuditLogEntry = await TryGetRelevantAuditLogEntry();
            }

        }
        catch (Exception ex)
        {
            var msg = $"Failed to fetch audit log records for user {user.FormatUsername()} in guild \"{guild.Name}\" (guildId={guild.Id}, userId={user.Id})";
            if (config.Enable || config.State == BanSyncGuildState.Active)
            {
                await _err.Submit(new ErrorReportBuilder()
                    .WithException(ex)
                    .WithNotes(msg)
                    .WithUser(user)
                    .WithGuild(guild));
            }
            else
            {
                _log.Warn(ex, msg);
            }
        }

        BanSyncRecordModel? info = null;
        UserPartialSnapshotModel? partialUserInfo = null;
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            partialUserInfo = new UserPartialSnapshotModel()
            {
                UserId = user.Id.ToString(),
                Username = user.Username,
                Discriminator = user.DiscriminatorValue == 0 ? null : user.Discriminator,
                DisplayName = string.IsNullOrEmpty(user.GlobalName) ? user.Username : user.GlobalName
            };
            info = new BanSyncRecordModel()
            {
                UserId = user.Id.ToString(),
                GuildId = guild.Id.ToString(),
                UserPartialSnapshotId = partialUserInfo.Id,
                GuildName = guild.Name,
                Reason = ParseReason(banInfo?.Reason)
            };
            AuditLogBanCacheModel? auditLogModel = null;
            if (mostRelevantAuditLogEntry != null)
            {
                auditLogModel = new AuditLogBanCacheModel(mostRelevantAuditLogEntry)
                {
                    GuildId = info.GuildId
                };
                info.AuditLogBanEntryId = auditLogModel.Id;
                info.BannedByUserId = auditLogModel.PerformedByUserId;
                await _auditLogEntryRepository.InsertOrUpdate(db, auditLogModel);
            }
            await db.UserPartialSnapshots.AddAsync(partialUserInfo);
            await _bansyncRecordsRepository.InsertOrUpdate(db, info);

            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            throw new BanSyncDbAddRecordFailureException(
                $"Failed to insert {nameof(BanSyncRecordModel)} and {nameof(UserPartialSnapshotModel)} into database.",
                ex,
                user, guild,
                info, partialUserInfo);
        }

        if (config?.State != BanSyncGuildState.Active)
            return;

        try
        {
            await NotifyBan(info);
        }
        catch (Exception ex)
        {
            throw new BanSyncNotifyFailureException(
                $"Failed to notify guilds about {user} ({user.Id}) being banned in \"{guild.Name}\" ({guild.Id})",
                ex, info, null, null, null);
        }

        return;
        async Task<RestAuditLogEntry?> TryGetRelevantAuditLogEntry()
        {
            var relatedAuditLogEntries = new List<RestAuditLogEntry>();
            foreach (var page in await guild.GetAuditLogsAsync(50,
                actionType: ActionType.Ban).ToListAsync())
            {
                relatedAuditLogEntries.AddRange(page);
            }

            return relatedAuditLogEntries.OrderByDescending(e => e.CreatedAt)
                .FirstOrDefault(entry => (DateTimeOffset.UtcNow - entry.CreatedAt) <= TimeSpan.FromMinutes(1));
        }
    }

    /// <summary>
    /// Notify all guilds that the user is in that the user has been banned.
    /// </summary>
    public async Task NotifyBan(BanSyncRecordModel info)
    {
        var exceptionList = new List<Exception>();
        foreach (var guild in _client.Guilds)
        {
            var guildConfig = await _bansyncGuildRepository.GetAsync(guild.Id);
            if (guildConfig?.State != BanSyncGuildState.Active)
                continue;

            SocketGuildUser? guildUser = null;
            try
            {
                guildUser = guild.GetUser(info.GetUserId());
            }
            catch { }
            if (guildUser == null) continue;

            try
            {
                await NotifyGuildAboutMutualBan(guildConfig, info, guild, guildUser);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to process record {info.Id} for user {info.UserPartialSnapshot.Username} ({info.UserId}) in guild \"{guild.Name}\" ({guild.Id})");
                exceptionList.Add(new BanSyncNotifyFailureException(
                    "Failed to notify guild about mutual ban",
                    ex,
                    info, guildConfig, guild, guildUser));
            }
        }
        if (exceptionList.Count == 1)
            throw exceptionList[0];
        else if (exceptionList.Count > 1)
            throw new AggregateException(
                $"Failed to notify multiple guilds for BanSyncRecord with Id={info.Id} (user: {info.UserId}, source guild: {info.GuildId})",
                exceptionList);
    }

    private async Task NotifyGuildAboutMutualBan(
        BanSyncGuildModel guildConfig,
        BanSyncRecordModel info,
        SocketGuild guild,
        SocketGuildUser guildUser)
    {
        var textChannel = guild.GetTextChannel(guildConfig.GetLogChannelId() ?? 0);
        var guildName = info.GuildName.Replace("`", "\\`");
        var embed = new EmbedBuilder()
        .WithTitle("User in your server just got banned")
        .WithDescription($"<@{info.UserId}> (`{info.UserPartialSnapshot.Username}`) just got banned from `{guildName}` at <t:{new DateTimeOffset(info.CreatedAt).ToUnixTimeSeconds()}:F>");

        MemoryStream? reasonMemoryStream = null;
        if (info.Reason?.Length >= 1024)
        {
            reasonMemoryStream = new(System.Text.Encoding.UTF8.GetBytes(info.Reason));
        }
        else if (!string.IsNullOrEmpty(info.Reason?.Trim()))
        {
            embed.AddField("Reason", info.Reason.Trim());
        }

        try
        {
            if (reasonMemoryStream != null)
            {
                await textChannel.SendFileAsync(
                    new FileAttachment(reasonMemoryStream, "reason.txt"),
                    embed: embed.Build());
                try
                {
                    await reasonMemoryStream.DisposeAsync();
                }
                catch (Exception ex)
                {
                    _log.Warn(ex, $"Failed to dispose reason attachment stream for channel \"{textChannel.Name}\" ({textChannel.Id}) in guild \"{guild.Name}\" ({guild.Id}) to notifiy about user \"{info.UserPartialSnapshot.Username}\" ({info.UserId}) getting banned.");
                }
            }
            else
            {
                await textChannel.SendMessageAsync(
                    embed: embed.Build());
            }
        }
        catch (Exception ex)
        {
            var msg = $"Failed to notify guild {guild.Name} ({guild.Id}) of user {guildUser.Username} ({guildUser.Id}) ban record.";
            _log.Error(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithGuild(guild)
                .WithUser(guildUser)
                .WithChannel(textChannel)
                .AddSerializedAttachment("bansyncGuild.json", guildConfig)
                .AddSerializedAttachment("bansyncRecord.json", info));
        }
    }

    private async Task DiscordClientOnUserJoined(SocketGuildUser arg)
    {
        var guildConfig = await _bansyncGuildRepository.GetAsync(arg.Guild.Id);

        // Check if the guild has config stuff setup
        // If not then we just ignore
        if (guildConfig?.State != BanSyncGuildState.Active)
            return;

        // Check if config channel has been made, if not then ignore
        SocketTextChannel? logChannel = arg.Guild.GetTextChannel(guildConfig.GetLogChannelId().GetValueOrDefault(0));
        if (logChannel == null) return;

        // Check if this user has been banned before, if not then ignore
        var userInfo = await _bansyncRecordsRepository.GetInfoEnumerable(arg.Id,
            new BanSyncRecordRepository.QueryOptions()
            {
                IncludeGhostedRecords = false,
                IncludeUserPartialSnapshot = true
            });
        if (userInfo.Count < 1) return;

        // Create embed then send message in log channel.
        var embed = await GenerateEmbed(userInfo);
        await logChannel.SendMessageAsync(embed: embed.Build());
    }

    public async Task<EmbedBuilder> GenerateEmbed(ICollection<BanSyncRecordModel> data)
    {
        var sortedData = data.OrderByDescending(v => v.CreatedAt).ToArray();
        var last = sortedData[^1];
        var userId = last.GetUserId();
        var user = await _client.GetUserAsync(userId);
        var embed = new EmbedBuilder()
            .WithTitle("User has been banned previously")
            .WithColor(Color.Red);
        var name = user.Username ?? last.UserPartialSnapshot.Username ?? "<Unknown Username>";
        var discriminator = user.Discriminator ?? last?.UserPartialSnapshot.Discriminator;
        var usernameFormatted = discriminator == null ? name : $"{name}#{discriminator}";
        var guildCount = sortedData.Length == 0 ? "no" : sortedData.Length.ToString("n0");
        var guildCountPlural = sortedData.Length == 1 ? "" : "s";
        embed.WithDescription($"User {usernameFormatted} ({user.Id}) has been banned from {guildCount} guild{guildCountPlural}.");

        for (int i = 0; i < Math.Min(sortedData.Length, 25); i++)
        {
            var item = sortedData[i];
            var reason = item.Reason?.Trim()?.Replace("`", "\\`");
            var ts = $"-# <t:{new DateTimeOffset(item.CreatedAt).ToUnixTimeSeconds()}:F>";
            var maxLength = FieldContentBanSyncReasonMaxLength(reason, ts) - 3;
            if (reason != null && reason.Length > maxLength)
            {
                reason = reason[..maxLength] + "...";
            }

            var ra = reason == null
                ? DefaultReasonText
                : string.Format(FieldContentReasonTemplate, reason);
            var fieldContent = string.Join("\n", ra, ts);
            embed.AddField(
                item.GuildName,
                fieldContent,
                true);
        }

        return embed;
    }

    private const string DefaultReasonText = "*No reason provided*";
    private const string FieldContentReasonTemplate = "```\n{0}\n```";
    private static int FieldContentBanSyncReasonMaxLength(
        string? reason,
        string ts)
    {
        var nonUserFildContent = string.Join("\n",
            string.IsNullOrEmpty(reason) ? DefaultReasonText : string.Format(FieldContentReasonTemplate, ""),
            ts);
        return 1024 - nonUserFildContent.Length;
    }

    public async Task<BanSyncGuildKind> GetGuildKind(ulong guildId)
    {
        var guild = _client.GetGuild(guildId);

        var model = await _bansyncGuildRepository.GetAsync(guildId)
            ?? new(guildId);

        var selfMember = guild.GetUser(_client.CurrentUser.Id);

        var logChannelId = model.GetLogChannelId();
        if (!logChannelId.HasValue)
            return BanSyncGuildKind.LogChannelMissing;

        SocketTextChannel logChannel;
        try
        {
            logChannel = guild.GetTextChannel(logChannelId.Value);
            if (logChannel == null)
            {
                return BanSyncGuildKind.LogChannelMissing;
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to get channel {logChannelId} in Guild {guild.Name} ({guildId})");
            return BanSyncGuildKind.LogChannelCannotAccess;
        }

        try
        {
            var permissions = selfMember.GetPermissions(logChannel);
            if (!permissions.Has(ChannelPermission.SendMessages))
            {
                return BanSyncGuildKind.LogChannelCannotSendMessages;
            }
            else if (!permissions.Has(ChannelPermission.EmbedLinks))
            {
                return BanSyncGuildKind.LogChannelCannotSendEmbeds;
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to check for own permissions in channel {logChannel.Name} ({logChannel.Id}) from guild {guild.Name} ({guild.Id})");
            return BanSyncGuildKind.InternalError;
        }

        if (model is { State: BanSyncGuildState.Blacklisted })
            return BanSyncGuildKind.Blacklisted;

        if (guild.CreatedAt > DateTimeOffset.UtcNow.AddMonths(-6))
            return BanSyncGuildKind.TooYoung;
        else if (guild.MemberCount < 35)
            return BanSyncGuildKind.NotEnoughMembers;
        else
            return BanSyncGuildKind.Valid;
    }

    /// <summary>
    /// Set guild state and write to log channel.
    /// </summary>
    /// <param name="guildId">Target Guild snowflake</param>
    /// <param name="state">New state for the guild config</param>
    /// <param name="reason">Required when <paramref name="state"/> is <see cref="BanSyncGuildState.Blacklisted"/> or <see cref="BanSyncGuildState.RequestDenied"/></param>
    /// <returns>Updated model.</returns>
    /// <exception cref="InvalidOperationException">When <paramref name="reason"/> is empty when required.</exception>
    public async Task<BanSyncGuildModel?> SetGuildState(
        ulong guildId,
        BanSyncGuildState state,
        string reason = "",
        bool doRefreshBans = true)
    {
        var config = await _bansyncGuildRepository.GetAsync(guildId);
        if (config == null)
            return null;

        var oldConfig = await _bansyncGuildRepository.GetAsync(guildId);

        reason = string.IsNullOrEmpty(reason?.Trim()) ? "" : reason.Trim();
        if (state == BanSyncGuildState.Blacklisted || state == BanSyncGuildState.RequestDenied)
        {
            if (reason?.Length < 1)
                throw new InvalidOperationException($"Reason parameter is required (GuildId={guildId}, State={state})");

            config.Notes = reason;
            config.Enable = false;
        }
        else if (state == BanSyncGuildState.Active)
        {
            config.Notes = reason;
        }

        config.Enable = state == BanSyncGuildState.Active;
        config.State = state;

        await _bansyncGuildRepository.InsertOrUpdate(config);
        
        await SetGuildState_Notify(config);
        await SetGuildState_NotifyGuild(config, oldConfig);

        if (state == BanSyncGuildState.Active && doRefreshBans)
        {
            try
            {
                await RefreshBans(_client.GetGuild(guildId));
            }
            catch (Exception ex)
            {
                SocketGuild? guild = null;
                try
                {
                    guild = _client.GetGuild(guildId);
                }
                catch (Exception gex)
                {
                    _log.Error(gex, $"Failed to fetch Guild with Id {guildId}");
                }
                await _err.Submit(new ErrorReportBuilder()
                    .WithException(ex)
                    .WithGuild(guild)
                    .WithNotes($"Failed to refresh bans in guild \"{guild?.Name}\" ({guildId})")
                    .AddSerializedAttachment("bansyncGuild.json", config));
            }
        }

        return config;
    }
    
    protected async Task SetGuildState_Notify(BanSyncGuildModel model)
    {
        try
        {
            var guild = _client.GetGuild(model.GetGuildId());
            var logGuild = _client.GetGuild(_configData.BanSync.GuildId);
            var logChannel = logGuild.GetTextChannel(_configData.BanSync.LogChannelId);

            var embed = new EmbedBuilder()
                .WithTitle("SetGuildState")
                .WithUrl(_configData.HasDashboard ? $"{_configData.DashboardUrl}/Admin/Server/{model.GetGuildId()}#settings" : "")
                .AddField("Guild Info", string.Join("\n",
                    "```",
                    $"Id: {model.GetGuildId()}",
                    "Name: " + guild?.Name?.Replace("`", "\\`"),
                    $"Created At: {guild?.CreatedAt}",
                    $"Owner: {guild?.Owner} ({guild?.OwnerId})",
                    $"Members: {guild?.MemberCount}",
                    "```"))
                .AddField("State", model.State, true)
                .AddField("Reason", model.Notes ?? "", true)
                .WithCurrentTimestamp();

            await logChannel.SendMessageAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            await _err.Submit(new ErrorReportBuilder()
                .WithNotes($"Failed to tell bot owner about guild state change for GuildId={model.GuildId} (state changed to {model.State})")
                .WithException(ex)
                .AddSerializedAttachment("bansyncGuild.json", model));
        }
    }

    /// <summary>
    /// Notify guild owner when the BanSync state for their guild has changed.
    /// </summary>
    /// <param name="current">Current model content in db.</param>
    /// <param name="previous">Previous model in db before the change was made.</param>
    protected async Task SetGuildState_NotifyGuild(BanSyncGuildModel current, BanSyncGuildModel? previous)
    {
        SocketGuild guild;
        SocketTextChannel channel;
        try
        {
            guild = _client.GetGuild(current.GetGuildId())
                ?? throw new InvalidOperationException($"Failed to get Guild {current.GuildId}");
        }
        catch (Exception ex)
        {
            var msg = $"Failed to Guild with Id {current.GuildId}";
            _log.Error(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .AddSerializedAttachment("bansyncGuild.current.json", current)
                .AddSerializedAttachment("bansyncGuild.previous.json", previous));
            return;
        }
        try
        {
            channel = guild.GetTextChannel(current.GetLogChannelId() ?? 0)
                ?? throw new InvalidOperationException($"Failed to get Channel {current.LogChannelId} in Guild \"{guild.Name}\" ({guild.Id})");
        }
        catch (Exception ex)
        {
            var msg = $"Failed to get channel {current.LogChannelId} in Guild \"{guild.Name}\" ({guild.Id})";
            _log.Error(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithGuild(guild)
                .WithNotes(msg)
                .AddSerializedAttachment("bansyncGuild.current.json", current)
                .AddSerializedAttachment("bansyncGuild.previous.json", previous));
            return;
        }

        var embed = new EmbedBuilder()
            .WithCurrentTimestamp()
            .WithTitle("Ban Sync Notification");
        var baseServerMsg = $"<@{guild.OwnerId}> An update about BanSync in your server.";
        var baseDmMsg =
            $"An update about BanSync in your server, [`{guild.Name}`](https://discord.com/channels/{guild.Id}/)";

        var contact = $"[join our support server]({_configData.SupportServerUrl})";
        if (!Uri.TryCreate(_configData.SupportServerUrl, UriKind.Absolute, out var _))
        {
            contact = "[send me an email](mailto:kate@dariox.club)";
        }

        if (current.State == BanSyncGuildState.Active)
        {
            if (previous?.State == BanSyncGuildState.RequestDenied)
            {
                // mind has been changed
                embed.WithColor(Color.Green)
                    .WithDescription(
                        $"A mistake may have been made when reviewing your server. Your guild is now approved for BanSync.");
            }
            else if (previous?.State == BanSyncGuildState.Blacklisted)
            {
                // blacklist removed
                embed.WithColor(Color.Green)
                    .WithDescription(
                        $"Your guild has been removed from the blacklist and BanSync has been re-enabled.");
            }
            else
            {
                // bansync added
                var description = string.Join("\n",
                    "Congratulations! The BanSync feature was approved for usage in your server. " +
                    "All banned members have been synchronized on our side and you can see members in your server with an existing history on the dashboard.",
                    "",
                    $"If you need any assistance. Feel free to {contact}.");
                if (_configData.HasDashboard)
                    description += $"\n\nIf you would like to check mutual records in your server, you can do so [via the dashboard]({_configData.DashboardUrl}/Server/{guild.Id}/Moderation).";
                embed.WithColor(Color.Green)
                    .WithDescription(description);              
            }
        }
        else if (current.State == BanSyncGuildState.Blacklisted)
        {
            if (previous?.State == BanSyncGuildState.PendingRequest)
            {
                // rejected and blacklisted
                embed.WithColor(Color.Red)
                    .WithDescription(
                        $"Your request to enable BanSync has been rejected and your guild has been blacklisted. If you would like to appeal, then you can {contact}.");
            }
            else if (previous?.State != BanSyncGuildState.Blacklisted)
            {
                // blacklisted
                embed.WithColor(Color.Red)
                    .WithDescription($"Your guild has been blacklisted to use the BanSync feature. If you would like to appeal, then you can {contact}.");
            }
        }
        else if (current.State == BanSyncGuildState.RequestDenied)
        {
            if (previous?.State != BanSyncGuildState.RequestDenied)
            {
                // request has been denied
                embed.WithColor(Color.Red)
                    .WithDescription(
                        $"Your request to enable the BanSync feature has been denied. For more information, {contact}");
            }
        }
        else if (current.State == BanSyncGuildState.PendingRequest)
        {
            if (previous?.State != BanSyncGuildState.PendingRequest)
            {
                // awaiting approval
                embed.WithColor(Color.Blue)
                    .WithDescription(
                        $"The BanSync feature has been requested for your server. Please wait 24-48hr for our admin team to review your server. \n\n" +
                        $"***If it takes longer than that***, then please {contact}.");
            }
        }
        else
        {
            if (previous?.State == BanSyncGuildState.Active)
            {
                // awaiting approval
                embed.WithColor(Color.Blue)
                    .WithDescription($"The BanSync feature has been disabled in your server. Please {contact} for more information.");
            }
            else
            {
                return;
            }
        }

        try
        {
            await channel.SendMessageAsync(
                baseServerMsg, embed: embed.Build());
        }
        catch (Exception ex)
        {
            var msg = $"Failed to send message in BanSync log channel \"{channel.Name}\" ({channel.Id}) for Guild \"{guild.Name}\" ({guild.Id})";
            _log.Error(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithChannel(channel)
                .WithGuild(guild)
                .AddSerializedAttachment("bansyncGuild.current.json", current)
                .AddSerializedAttachment("bansyncGuild.previous.json", previous));
        }

        IDMChannel ownerDmsChannel;
        try
        {
            ownerDmsChannel = await guild.Owner.CreateDMChannelAsync();
        }
        catch (Exception ex)
        {
            var msg = $"Failed to Create DM Channel for {guild.Owner} ({guild.OwnerId}), who is the owner of \"{guild.Name}\" ({guild.Id})";
            _log.Error(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithUser(guild.Owner)
                .WithGuild(guild)
                .AddSerializedAttachment("bansyncGuild.current.json", current)
                .AddSerializedAttachment("bansyncGuild.previous.json", previous));
            return;
        }
        try
        {
            await ownerDmsChannel.SendMessageAsync(
                baseDmMsg,
                embed: embed.Build());
        }
        catch (Exception ex)
        {
            var msg = $"Failed to send message to {guild.Owner} ({guild.Owner.Id}), who is the owner of \"{guild.Name}\" ({guild.Id})";
            _log.Error(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithChannel(ownerDmsChannel)
                .WithGuild(guild)
                .AddSerializedAttachment("bansyncGuild.current.json", current)
                .AddSerializedAttachment("bansyncGuild.previous.json", previous));
        }

    }

    /// <summary>
    /// Request for BanSync to be enabled on the guild specified.
    /// </summary>
    /// <param name="guildId">GuildId to request the BanSync feature on.</param>
    /// <returns>Updated <see cref="ConfigBanSyncModel"/></returns>
    public async Task<BanSyncGuildModel> RequestGuildEnable(ulong guildId)
    {
        var config = await _bansyncGuildRepository.GetAsync(guildId)
            ?? new(guildId);
        // When state is blacklisted/denied/pending, reject
        if (config.State == BanSyncGuildState.Blacklisted ||
            config.State == BanSyncGuildState.RequestDenied ||
            config.State == BanSyncGuildState.PendingRequest)
        {
            return config;
        }

        // Ignore when log channel is missing or we can't access it.
        var guildState = await GetGuildKind(guildId);
        if (guildState == BanSyncGuildKind.LogChannelMissing ||
            guildState == BanSyncGuildKind.LogChannelCannotAccess ||
            guildState == BanSyncGuildKind.LogChannelCannotSendMessages ||
            guildState == BanSyncGuildKind.LogChannelCannotSendEmbeds ||
            guildState == BanSyncGuildKind.TooYoung ||
            guildState == BanSyncGuildKind.NotEnoughMembers ||
            guildState == BanSyncGuildKind.Blacklisted ||
            guildState == BanSyncGuildKind.InternalError) // also return on internal error
            return config;

        config.State = BanSyncGuildState.PendingRequest;
        await _bansyncGuildRepository.InsertOrUpdate(config);

        await RequestGuildEnable_SendNotification(config);

        config = await _bansyncGuildRepository.GetAsync(guildId);

        return config ?? throw new InvalidOperationException($"Result model with type {typeof(BanSyncGuildModel)} is null when it couldn't be where GuildId={guildId})");
    }

    /// <summary>
    /// Send notification to <see cref="BanSyncConfigItem.RequestChannelId."/> that a server has requested the BanSync feature.
    /// </summary>
    protected async Task RequestGuildEnable_SendNotification(BanSyncGuildModel model)
    {
        var guild = _client.GetGuild(model.GetGuildId());
        var logGuild = _client.GetGuild(_configData.BanSync.GuildId);
        var logRequestChannel = logGuild.GetTextChannel(_configData.BanSync.RequestChannelId);
        // Fetch first text channel to create invite for
        var firstTextChannel = guild.Channels.OfType<ITextChannel>().FirstOrDefault();

        // Generate invite from firstTextChannel and fetch the URL for the invite
        string? inviteUrl = null;
        if (firstTextChannel != null)
        {
            try
            {
                IInviteMetadata? invite = null;
                if (firstTextChannel != null)
                    invite = await firstTextChannel.CreateInviteAsync(null);
                inviteUrl = invite?.Url ?? "none";
            }
            catch (Exception ex)
            {
                _log.Warn(ex, $"Failed to create invite for channel {firstTextChannel?.Name} ({firstTextChannel?.Id}) in guild \"{guild.Name}\" ({guild.Id})");
            }
        }

        await logRequestChannel.SendMessageAsync(embed: new EmbedBuilder()
        {
            Title = "BanSync Request Received.",
            Description = string.Join("\n",
                "```",
                $"Id: {guild.Id}",
                $"Name: {guild.Name}",
                $"Owner: {guild.Owner} ({guild.Owner.Id})",
                $"Member Count: {guild.MemberCount}",
                $"Invite: {inviteUrl}",
                "```"
            ),
            Url = _configData.HasDashboard ? $"{_configData.DashboardUrl}/Admin/Server/{guild.Id}#settings" : ""
        }.Build());
    }
}
