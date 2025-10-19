using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NLog;
using XeniaBot.Shared;
using XeniaBot.Shared.Config;
using XeniaBot.Shared.Services;
using XeniaDiscord.Common.Interfaces;
using XeniaDiscord.Common.Repositories;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.BanSync;

namespace XeniaDiscord.Common.Services;

public class BanSyncService : IBanSyncService
{
    private readonly DiscordSocketClient _client;
    private readonly ApplicationDbContext _db;
    private readonly BanSyncRepository _bsRepo;
    private readonly BanSyncGuildRepository _bsGuildRepo;
    private readonly XeniaConfig _config;
    private readonly ErrorReportService _err;
    private readonly ProgramDetails _programDetails;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public BanSyncService(IServiceProvider services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        _db = services.GetRequiredService<ApplicationDbContext>();
        _bsRepo = services.GetRequiredService<BanSyncRepository>();
        _bsGuildRepo = services.GetRequiredService<BanSyncGuildRepository>();
        _config = services.GetRequiredService<XeniaConfig>();
        _err = services.GetRequiredService<ErrorReportService>();
        _programDetails = services.GetRequiredService<ProgramDetails>();

        if (_programDetails.Platform != XeniaPlatform.WebPanel)
        {
            _client.UserJoined += _client_UserJoined;
            _client.UserBanned += _client_UserBanned;
        }
    }

    public async Task UpdateLogChannel(IGuild guild, ITextChannel channel, IUser updatedBy)
    {
        try
        {
            var config = await _bsGuildRepo.GetAsync(guild.Id)
            ?? new BanSyncGuildModel()
            {
                Id = guild.Id.ToString(),
                GuildName = guild.Name
            };
            config.LogChannelId = channel.Id.ToString();
            await _bsGuildRepo.UpdateAsync(config, updatedBy);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to update log channel to {channel.Name} ({channel.Id}) for guild \"{guild.Name}\" ({guild.Id}), which was updated by \"{updatedBy.GlobalName}\" ({updatedBy.Username}, {updatedBy.Id})");
            throw;
        }
    }

    public async Task RefreshBans(SocketGuild guild)
    {
        var config = await _bsGuildRepo.GetAsync(guild.Id)
            ?? new BanSyncGuildModel()
            {
                Id = guild.Id.ToString(),
                Enabled = false,
                State = BanSyncGuildState.Unknown
            };
        if (!config.Enabled || config.State != BanSyncGuildState.Active) return;

        var bans = await guild.GetBansAsync(1000000).FlattenAsync().ConfigureAwait(false);
        await using var ctx = _db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            foreach (var ban in bans)
            {

                try
                {
                    if (await _bsRepo.AnyAsync(guild, ban)) continue;

                    var model = new BanSyncRecordModel()
                    {
                        UserId = ban.User.Id.ToString(),
                        Username = ban.User.Username,
                        DisplayName = ban.User.GlobalName,
                        GuildId = guild.Id.ToString(),
                        GuildName = guild.Name,
                        Reason = ban.Reason
                    };
                    await ctx.BanSyncRecords.AddAsync(model);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to insert ban for user \"{ban.User}\" ({ban.User.Id}) in Guild \"{guild.Name}\" ({guild.Id})", ex);
                }
            }
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            _log.Error(ex, $"Failed to refresh bans for Guild \"{guild.Name}\" ({guild.Id})");
        }
    }

    /// <summary>
    /// Add user to database and notify mutual servers. <see cref="NotifyBan(BanSyncInfoModel)"/>
    /// </summary>
    private async Task _client_UserBanned(SocketUser user, SocketGuild guild)
    {
        // Ignore if guild config is disabled
        var config = await _bsGuildRepo.GetAsync(guild.Id)
            ?? new BanSyncGuildModel()
            {
                Id = guild.Id.ToString(),
                GuildName = guild.Name,
                Enabled = false,
                State = BanSyncGuildState.Unknown
            };
        if (!config.Enabled || config.State != BanSyncGuildState.Active) return;

        var banInfo = await guild.GetBanAsync(user);
        var model = await _bsRepo.CreateAsync(guild, banInfo);
        await NotifyBan(model);
    }
    private async Task _client_UserJoined(SocketGuildUser arg)
    {
        var guildConfig = await _bsGuildRepo.GetAsync(arg.Guild.Id);

        // Check if the guild has config stuff setup
        // If not then we just ignore
        if (guildConfig == null)
            return;

        if (guildConfig.State != BanSyncGuildState.Active)
            return;

        // Check if config channel has been made, if not then ignore
        var logChannel = arg.Guild.GetTextChannel(guildConfig.GetLogChannelId() ?? 0);
        if (logChannel == null)
            return;

        // Check if this user has been banned before, if not then ignore
        if (!await _bsRepo.AnyAsync(arg.Guild, arg)) return;

        var totalCount = await _bsRepo.GetCountForUser(arg);
        var info = await _bsRepo.GetAllForUser(arg, limit: 25);

        // Create embed then send message in log channel.
        var embed = await GenerateEmbed(info, totalCount);
        await logChannel.SendMessageAsync(embed: embed.Build());
    }

    /// <summary>
    /// Notify all guilds that the user is in that the user has been banned.
    /// </summary>
    public async Task NotifyBan(BanSyncRecordModel info)
    {
        var taskList = new List<Task>();
        foreach (var g in _client.Guilds)
        {
            var guild = g;
            var guildUser = guild.GetUser(info.GetUserId() ?? 0);
            if (guildUser == null)
                continue;

            var guildConfig = await _bsGuildRepo.GetAsync(guild.Id);
            if (guildConfig == null || guildConfig.State != BanSyncGuildState.Active)
                continue;

            var logChannelId = guildConfig.GetLogChannelId();
            if (!logChannelId.HasValue) continue;
            async Task NotifyTask(ulong channelId)
            {
                var textChannel = guild.GetTextChannel(channelId);
                var embed = new EmbedBuilder()
                {
                    Title = "User in your server just got banned",
                    Description = $"<@{info.UserId}> just got banned from `{info.GuildName}` at <t:{new DateTimeOffset(info.CreatedAt).ToUnixTimeSeconds()}:F>",
                };
                if (!string.IsNullOrEmpty(info.Reason?.Trim()))
                {
                    embed.AddField("Reason", info.Reason.Trim());
                }
                try
                {
                    await textChannel.SendMessageAsync(embed: embed.Build());
                }
                catch (Exception ex)
                {
                    var msg = $"Failed to notify guild \"{guild.Name}\" ({guild.Id}) about BanSync Record {info.Id} ({info.Username} in {info.GuildName} {info.GuildId})";
                    _log.Error(ex, msg);
                    await _err.ReportException(ex, msg);
                }
            }
            taskList.Add(NotifyTask(logChannelId.Value));
        }
        await Task.WhenAll(taskList);
    }

    public async Task<EmbedBuilder> GenerateEmbed(ICollection<BanSyncRecordModel> data, long totalCount)
    {
        var sortedData = data.OrderByDescending(v => v.CreatedAt).ToArray();
        var first = sortedData.FirstOrDefault();
        var userId = first?.GetUserId();
        var user = await _client.GetUserAsync(userId ?? 0);
        var embed = new EmbedBuilder()
        {
            Title = "User has been banned previously",
            Color = Color.Red
        };
        var name = user.Username ?? first?.Username ?? "<Unknown Username>";
        var plural = totalCount == 1 ? "" : "s";
        var description = $"User {name} ({user.Id}) has been banned from {totalCount} guild{plural}.";
        if (totalCount > data.Count)
            description += $"\n-# **NOTE:** Only the most recent {data.Count} records are shown.";
        embed.WithDescription(description);

        for (int i = 0; i < Math.Min(sortedData.Length, 25); i++)
        {
            var item = sortedData[i];

            embed.AddField(
                item.GuildName,
                $"<t:{item.CreatedAt}:F> {item.Reason ?? string.Empty}"[..2000],
                true);
        }

        return embed;
    }

    public async Task<BanSyncGuildKind> GetGuildKind(ulong guildId)
    {
        var guild = _client.GetGuild(guildId);

        var guildConf = await _bsGuildRepo.GetAsync(guildId);
        if ((guildConf?.GetLogChannelId() ?? 0) == 0)
            return BanSyncGuildKind.LogChannelMissing;

        try
        { guild.GetTextChannel(guildConf?.GetLogChannelId() ?? 0); }
        catch
        { return BanSyncGuildKind.LogChannelCannotAccess; }
        if (guildConf is { State: BanSyncGuildState.Blacklisted })
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
    /// <returns></returns>
    /// <exception cref="Exception">When <paramref name="reason"/> is empty when required.</exception>
    public async Task<BanSyncGuildModel?> SetGuildState(ulong guildId,
        BanSyncGuildState state,
        string reason = "",
        bool doRefreshBans = true,
        IUser? updatedBy = null)
    {
        var config = await _bsGuildRepo.GetAsync(guildId);
        if (config == null)
            return null;

        var guild = _client.GetGuild(guildId);

        var oldConfig = config.Clone();

        config.GuildName = guild.Name;
        config.State = state;
        config.Enabled = state == BanSyncGuildState.Active;

        if (state == BanSyncGuildState.Blacklisted || state == BanSyncGuildState.RequestDenied)
        {
            config.InternalNote = reason;
            config.Enabled = false;
        }
        else if (state == BanSyncGuildState.Active)
        {
            config.InternalNote = reason;
        }

        config = await _bsGuildRepo.UpdateAsync(config, updatedBy);
        
        await SetGuildState_Notify(config);
        await SetGuildState_NotifyGuild(config, oldConfig);

        if (state == BanSyncGuildState.Active && doRefreshBans && config.State != oldConfig.State)
        {
            try
            {
                await RefreshBans(_client.GetGuild(guildId));
            }
            catch (Exception ex)
            {
                await _err.ReportException(ex, $"Failed to refresh bans in {guild.Name} ({guildId})");
            }
        }

        return config;
    }
    
    protected async Task SetGuildState_Notify(BanSyncGuildModel model)
    {
        if (!_config.Discord.BanSync.GuildId.HasValue)
        {
            _log.Error("BanSync Guild Id not set in config file");
            return;
        }
        if (!_config.Discord.BanSync.GuildStateChangedChannelId.HasValue)
        {
            _log.Error("BanSync \"Guild State Changed\" Channel Id not set in config file");
            return;
        }

        try
        {
            var guild = _client.GetGuild(model.GetGuildId() ?? 0);
            var logGuild = _client.GetGuild(_config.Discord.BanSync.GuildId.Value);
            var logChannel = logGuild.GetTextChannel(_config.Discord.BanSync.GuildStateChangedChannelId.Value);

            var embed = new EmbedBuilder()
            {
                Title = "SetGuildState",
                Description = string.Join("\n",
                    "```",
                    $"Guild: {guild.Name ?? "<null>"} ({model.Id})",
                    $"State: {model.State}",
                    "```"
                ),
                Url = HasDashboardUrl(_config) ? $"{_config.Dashboard.Url}/Admin/Guild/{guild.Id}#settings" : ""
            }.WithCurrentTimestamp();
            await logChannel.SendMessageAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            await _err.ReportException(ex, $"To notify bot owner about guild state change for {model.Id}");
        }
    }

    /// <summary>
    /// Notify guild owner when the BanSync state for their guild has changed.
    /// </summary>
    /// <param name="current">Current model content in db.</param>
    /// <param name="previous">Previous model in db before the change was made.</param>
    protected async Task SetGuildState_NotifyGuild(BanSyncGuildModel current, BanSyncGuildModel? previous)
    {
        var guild = _client.GetGuild(current.GetGuildId() ?? 0);
        var channel = guild.GetTextChannel(current.GetLogChannelId() ?? 0);
        if (channel == null)
        {
            _log.Warn($"Failed to get channel {current.LogChannelId} in guild {guild.Id} ({guild.Name})");
            return;
        }

        var embed = new EmbedBuilder()
            .WithCurrentTimestamp()
            .WithTitle("Ban Sync Notification");
        var baseServerMsg = $"<@{guild.OwnerId}> An update about BanSync in your server.";
        var baseDmMsg =
            $"An update about BanSync in your server, [`{guild.Name}`](https://discord.com/channels/{guild.Id}/)";

        var contact = $"[join our support server]({_config.SupportServerUrl})";

        if (current.State == BanSyncGuildState.Active)
        {
            if (previous?.State == BanSyncGuildState.RequestDenied)
            {
                // mind has been changed
                embed.WithColor(Color.Green)
                    .WithDescription(
                        $"A mistake may have been made by the admins. BanSync has been re-enabled for your guild.");
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
                var d =
                    "Congratulations! The BanSync feature was approved for usage in your server. All banned members have been synchronized on our side and you can see members in your server with an existing history on the dashboard.\n" +
                    "\n" +
                    $"If you need any assistance. Feel free to {contact}.";
                if (HasDashboardUrl(_config))
                    d += $"\n\nIf you would like to check mutual records in your server, you can do so [via the dashboard]({_config.Dashboard.Url}/Server/{guild.Id}/BanSync)";
                embed.WithColor(Color.Green)
                    .WithDescription(d);              
            }
        }
        else if (current.State == BanSyncGuildState.Blacklisted)
        {
            if (previous?.State == BanSyncGuildState.PendingRequest)
            {
                // rejected and blacklisted
                embed.WithColor(Color.Red)
                    .WithDescription(
                        $"Your request to enable BanSync has been rejected and your guild has been blacklisted. For more information, {contact} if you would like to appeal.");
            }
            else if (previous?.State != BanSyncGuildState.Blacklisted)
            {
                // blacklisted
                embed.WithColor(Color.Red)
                    .WithDescription(
                        $"Your guild has been blacklisted to use the BanSync feature. For more information, {contact} if you would like to appeal.");
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
                        $"***If it takes longer than that***, then {contact}.");
            }
        }
        else
        {
            if (previous?.State == BanSyncGuildState.Active)
            {
                // awaiting approval
                embed.WithColor(Color.Blue)
                    .WithDescription(
                        $"The BanSync feature has been disabled in your server. Please {contact} for more information.");
            }
            else
            {
                return;
            }
        }

        await channel.SendMessageAsync(
            baseServerMsg, embed: embed.Build());

        await guild.Owner.SendMessageAsync(
            baseDmMsg,
            embed: embed.Build());
    }
    /// <summary>
    /// Request for BanSync to be enabled on the guild specified.
    /// </summary>
    /// <param name="guildId">GuildId to request the BanSync feature on.</param>
    /// <returns>Updated <see cref="ConfigBanSyncModel"/></returns>
    public async Task<BanSyncGuildModel> RequestGuildEnable(ulong guildId, IUser? requestedBy = null)
    {
        var guild = _client.GetGuild(guildId);

        var config = await _bsGuildRepo.GetAsync(guildId);
        if (config == null)
        {
            config = new BanSyncGuildModel()
            {
                Id = guildId.ToString(),
                GuildName = guild.Name,
            };
        }
        // When state is blacklisted/denied/pending, reject
        if (config.State == BanSyncGuildState.Blacklisted || config.State == BanSyncGuildState.RequestDenied || config.State == BanSyncGuildState.PendingRequest)
        {
            return config;
        }

        // Ignore when log channel is missing or we can't access it.
        var guildState = await GetGuildKind(guildId);
        if (guildState == BanSyncGuildKind.LogChannelMissing ||
            guildState == BanSyncGuildKind.LogChannelCannotAccess)
            return config;

        config.State = BanSyncGuildState.PendingRequest;
        config = await _bsGuildRepo.UpdateAsync(config, requestedBy);

        await RequestGuildEnable_SendNotification(config);

        return config;
    }
    /// <summary>
    /// Send notification to <see cref="BanSyncConfigItem.RequestChannelId."/> that a server has requested the BanSync feature.
    /// </summary>
    protected async Task RequestGuildEnable_SendNotification(BanSyncGuildModel model)
    {
        if (!_config.Discord.BanSync.GuildId.HasValue)
        {
            _log.Error("BanSync Guild Id not set in config file");
            return;
        }
        if (!_config.Discord.BanSync.FeatureRequestChannelId.HasValue)
        {
            _log.Error("BanSync \"Feature Request\" Channel Id not set in config file");
            return;
        }
        var guild = _client.GetGuild(model.GetGuildId() ?? 0);
        var logGuild = _client.GetGuild(_config.Discord.BanSync.GuildId.Value);
        var logRequestChannel = logGuild.GetTextChannel(_config.Discord.BanSync.FeatureRequestChannelId.Value);
        // Fetch first text channel to create invite for
        var firstTextChannel = guild.Channels.OfType<ITextChannel>().FirstOrDefault();

        // Generate invite from firstTextChannel and fetch the URL for the invite
        IInviteMetadata? invite = null;
        if (firstTextChannel != null)
            invite = await firstTextChannel.CreateInviteAsync(null);
        string inviteUrl = invite?.Url ?? "none";

        await logRequestChannel.SendMessageAsync(embed: new EmbedBuilder()
        {
            Title = "BanSync Request Received.",
            Description = string.Join("\n",
                "```",
                $"Id: {guild.Id}",
                $"Name: {guild.Name}",
                $"Owner: \"{guild.Owner.GlobalName}\" (username: \"{guild.Owner}\", id: {guild.Owner.Id})",
                $"Member Count: {guild.MemberCount}",
                $"Invite: {inviteUrl}",
                "```"),
            Url = HasDashboardUrl(_config) ? $"{_config.Dashboard.Url}/Admin/Guild/{guild.Id}#settings" : ""
        }.Build());
    }

    private static bool HasDashboardUrl(XeniaConfig config)
    {
        return config.Dashboard.Has && !string.IsNullOrEmpty(config.Dashboard.Url);
    }
}
