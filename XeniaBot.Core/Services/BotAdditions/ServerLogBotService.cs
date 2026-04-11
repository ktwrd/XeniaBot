using CSharpFunctionalExtensions;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XeniaBot.Core.Helpers;
using XeniaBot.Data.Models.Archival;
using XeniaBot.DiscordCache.Models;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;
using XeniaDiscord.Common.Services;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.ServerLog;
using XeniaDiscord.Data.Models.Snapshot;

using DiscordCacheService = XeniaBot.Core.Services.Wrappers.DiscordCacheService;

namespace XeniaBot.Core.Services.BotAdditions;

[XeniaController]
public class ServerLogBotService : BaseService
{
    private readonly Logger _log = LogManager.GetLogger("Xenia." + nameof(ServerLogBotService));
    private readonly DiscordSocketClient _discord;
    private readonly DiscordCacheService _discordCache;
    private readonly DiscordSnapshotService _discordSnapshot;
    private readonly ErrorReportService _errorService;
    private readonly XeniaDbContext _db;
    private readonly ServerLogService _serverLogService;
    public ServerLogBotService(IServiceProvider services)
        : base(services)
    {
        _discord = services.GetRequiredService<DiscordSocketClient>();
        _discordCache = services.GetRequiredService<DiscordCacheService>();
        _discordSnapshot = services.GetRequiredService<DiscordSnapshotService>();
        _errorService = services.GetRequiredService<ErrorReportService>();
        _serverLogService = services.GetRequiredService<ServerLogService>();
        _db = services.GetRequiredScopedService<XeniaDbContext>(out var _);

        var details = services.GetRequiredService<ProgramDetails>();

        if (details.Platform == XeniaPlatform.Bot)
        {
            _discord.UserJoined += DiscordOnUserJoined;
            _discord.UserLeft += DiscordOnUserLeft;
            _discord.UserBanned += DiscordOnUserBanned;
            _discord.UserUnbanned += DiscordOnUserUnbanned;

            _discord.MessageDeleted += DiscordOnMessageDelete;
            _discordCache.MessageChange += DiscordCacheMessageChangeUpdate;

            _discordSnapshot.GuildMemberUpdated += DiscordSnapshotGuildMemberUpdate;
            _discordSnapshot.GuildRoleUpdated += DiscordSnapshotGuildRoleUpdate;
        }
    }

    #region Role Events
    private async Task DiscordSnapshotGuildRoleUpdate(
        GuildRoleSnapshotModel? before,
        GuildRoleSnapshotModel model)
    {
        var guildIdStr = model.GuildId;
        if (before == null && model.SnapshotSource == GuildRoleSnapshotSource.RoleEdit)
        {
            _log.Trace($"Event skipped. No before state for Edit source (guildId={model.GuildId}, roleId={model.RoleId}, recordId={model.Id})");
            return;
        }
        await using var db = _db.CreateSession();
        DiscordSnapshotRoleUpdateInfo? info = null;
        try
        {
            // disabled in guild, ignore
            if (!await db.ServerLogGuilds.AnyAsync(e => e.GuildId == guildIdStr && e.Enabled))
            {
                _log.Trace($"Event skipped. Server Logging disabled in Guild (guildId={model.GuildId}, roleId={model.RoleId}, recordId={model.Id})");
                return;
            }

            var permissionsAddedList = new List<GuildPermission>();
            var permissionsRemovedList = new List<GuildPermission>();
            
            if (model.SnapshotSource == GuildRoleSnapshotSource.RoleDelete)
            {
                permissionsRemovedList.AddRange(model.Permissions.Select(e => e.GetValue()));
            }
            else if (model.SnapshotSource == GuildRoleSnapshotSource.RoleCreate)
            {
                permissionsAddedList.AddRange(model.Permissions.Select(e => e.GetValue()));
            }
            else if (before != null)
            {
                permissionsAddedList.AddRange(model.Permissions
                    .Where(a => !before.Permissions.Any(b => b.Value == a.Value))
                    .Select(e => e.GetValue()));
                permissionsRemovedList.AddRange(before.Permissions
                    .Where(b => !model.Permissions.Any(a => a.Value == b.Value))
                    .Select(e => e.GetValue()));
            }
            else
            {
                permissionsAddedList.AddRange(model.Permissions.Select(e => e.GetValue()));
            }
            info = new()
            {
                PermissionsAdded = permissionsAddedList,
                PermissionsRemoved = permissionsRemovedList,
                SnapshotBefore = before,
                Snapshot = model,
            };
            if (!info.Any) return;
            
            _log.Trace($"Handling event (source={model.SnapshotSource}, guildId={model.GuildId}, roleId={model.RoleId}, recordId={model.Id})");

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var embed = new EmbedBuilder()
                .WithDescription(string.Join("\n",
                    $"<@&{model.RoleId}>",
                    "Name: `" + model.Name?.Replace("`", "'") + "`"))
                .WithFooter("ID: " + model.RoleId)
                .WithColor(Color.Blue)
                .WithCurrentTimestamp();
            var attachments = new List<FileAttachment>();

            var targetEvent = model.SnapshotSource switch
            {
                GuildRoleSnapshotSource.RoleCreate => ServerLogEvent.RoleCreate,
                GuildRoleSnapshotSource.RoleEdit => ServerLogEvent.RoleEdit,
                GuildRoleSnapshotSource.RoleDelete => ServerLogEvent.RoleDelete,
                _ => Maybe<ServerLogEvent>.None
            };

            switch (model.SnapshotSource)
            {
                case GuildRoleSnapshotSource.RoleCreate:
                    embed.WithTitle("Role Created");
                    info.WithInfo(embed, attachments);
                    info.WithPermissionsUpdated(embed, attachments);
                    break;
                case GuildRoleSnapshotSource.RoleEdit:
                    embed.WithTitle("Role Updated")
                         .WithDescription(string.Join("\n",
                        $"<@&{model.RoleId}> was updated <t:{now}:R> (name: {model.Name?.Replace("`", "'")})",
                        "Name: `" + model.Name?.Replace("`", "'") + "`"));
                    info.WithInfo(embed, attachments);
                    info.WithPermissionsUpdated(embed, attachments);
                    break;
                case GuildRoleSnapshotSource.RoleDelete:
                    embed.WithTitle("Role Deleted");
                    info.WithPermissionsUpdated(embed, attachments);
                    break;
            }
            if (targetEvent.HasValue)
            {
                    await _serverLogService.EventHandle(
                        model.GetGuildId(),
                    targetEvent.Value,
                        [embed],
                        attachments);
            }
        }
        catch (Exception ex)
        {
            var msg = $"Failed to handle event for Guild {model.GuildId} and Role {model.RoleId} (SnapshotId={model.Id}, SnapshotSource={model.SnapshotSource})";
            _log.Error(ex, msg);
            await _errorService.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .AddSerializedAttachment("snapshotInfo.json", info)
                .AddSerializedAttachment("snapshot.before.json", before)
                .AddSerializedAttachment("snapshot.after.json", model));
        }
    }
    #endregion

    #region Member Updated
    private async Task DiscordSnapshotGuildMemberUpdate(
        GuildMemberSnapshotModel? before,
        GuildMemberSnapshotModel model)
    {
        if (model.SnapshotSource != GuildMemberSnapshotSource.MemberUpdate) return;

        var guildIdStr = model.GuildId;
        if (before == null)
        {
            _log.Trace($"Event. No before state (guildId={model.GuildId}, userId={model.UserId}, recordId={model.RecordId})");
            return;
        }
        await using var db = _db.CreateSession();
        DiscordSnapshotMemberUpdateInfo? info = null;
        try
        {
            // disabled in guild, ignore
            if (!await db.ServerLogGuilds.AnyAsync(e => e.GuildId == guildIdStr && e.Enabled)) return;

            var rolesAdded = new List<(ulong, GuildRoleSnapshotModel?)>();
            var rolesRemoved = new List<(ulong, GuildRoleSnapshotModel?)>();

            rolesAdded.AddRange(model.Roles
                .Where(a => !before.Roles.Any(b => b.RoleId == a.RoleId))
                .Select(e => (e.GetRoleId(), e.GuildRoleSnapshot)));

            rolesRemoved.AddRange(before.Roles
                .Where(b => !model.Roles.Any(a => a.RoleId == b.RoleId))
                .Select(e => (e.GetRoleId(), e.GuildRoleSnapshot)));

            var permissionsAddedList = new List<GuildPermission>();
            var permissionsRemovedList = new List<GuildPermission>();

            permissionsAddedList.AddRange(model.Permissions
                .Where(a => !before.Permissions.Any(b => b.Value == a.Value))
                .Select(e => e.GetValue()));
            permissionsRemovedList.AddRange(before.Permissions
                .Where(b => !model.Permissions.Any(a => a.Value == b.Value))
                .Select(e => e.GetValue()));

            ulong permissionsAddedRaw = 0;
            ulong permissionsRemovedRaw = 0;
            foreach (var value in permissionsAddedList)
            {
                permissionsAddedRaw |= (ulong)value;
            }
            foreach (var value in permissionsRemovedList)
            {
                permissionsRemovedRaw |= (ulong)value;
            }

            info = new()
            {
                RolesAdded = rolesAdded,
                RolesRemoved = rolesRemoved,
                PermissionsAdded = permissionsAddedList,
                PermissionsRemoved = permissionsRemovedList,
                SnapshotBefore = before,
                Snapshot = model
            };

            if (!info.AnyUpdates)
            {
                _log.Trace($"No permission/role updates for snapshot (RecordId={model.RecordId}, UserId={model.UserId}, GuildId={model.GuildId})");
                return;
            }
            
            var events = await _db.ServerLogChannels
                .Where(e => e.GuildId == guildIdStr)
                .Select(e => e.Event)
                .Distinct().ToListAsync();
            if (events.Contains(ServerLogEvent.MemberRoleAdded) ||
                events.Contains(ServerLogEvent.MemberRoleRemoved) ||
                events.Contains(ServerLogEvent.MemberRoleUpdated) ||
                events.Contains(ServerLogEvent.MemberPermissionsUpdated))
            {
                if (events.Contains(ServerLogEvent.MemberRoleUpdated) &&
                    !events.Contains(ServerLogEvent.MemberRoleAdded) &&
                    !events.Contains(ServerLogEvent.MemberRoleRemoved))
                {
                    await SendAs(GetRolesUpdated(), ServerLogEvent.MemberRoleUpdated);
                }
                else
                {
                    if (info.RolesAdded.Count > 0)
                    {
                        await SendAs(GetRolesAdded(), ServerLogEvent.MemberRoleAdded);
                    }
                    if (info.RolesRemoved.Count > 0)
                    {
                        await SendAs(GetRolesRemoved(), ServerLogEvent.MemberRoleRemoved);
                    }
                }
                if (info.AnyPermissions)
                {
                    var (permEmbed, permAtt) = GetPermissionsUpdated();
                    await _serverLogService.EventHandle(model.GetGuildId(), ServerLogEvent.MemberPermissionsUpdated, [permEmbed], permAtt);
                }
            }
            else if (events.Contains(ServerLogEvent.Fallback) || events.Contains(ServerLogEvent.MemberUpdated))
            {
                await SendAs(GetCombined(), ServerLogEvent.MemberUpdated);
            }

            async Task SendAs((EmbedBuilder, List<FileAttachment>) tuple, ServerLogEvent @event)
            {
                var (embed, att) = tuple;
                await _serverLogService.EventHandle(model.GetGuildId(), @event, [embed], att);
            }
            (EmbedBuilder, List<FileAttachment>) GetCombined()
            {
                var att = new List<FileAttachment>();
                var embed = CreateEmbed(model)
                    .WithTitle("Member Updated");
                info.WithPermissionsUpdated(embed, att);
                info.WithRolesAdded(embed, att);
                info.WithRolesRemoved(embed, att);
                return (embed, att);
            }
            (EmbedBuilder, List<FileAttachment>) GetRolesUpdated()
            {
                var att = new List<FileAttachment>();
                var embed = CreateEmbed(model)
                    .WithTitle("User Roles Updated");
                info.WithRolesAdded(embed, att);
                info.WithRolesRemoved(embed, att);
                return (embed, att);
            }
            (EmbedBuilder, List<FileAttachment>) GetRolesAdded()
            {
                var att = new List<FileAttachment>();
                var embed = CreateEmbed(model)
                        .WithTitle("User Roles Added")
                        .WithColor(Color.Blue);
                info.WithRolesAdded(embed, att);
                return (embed, att);
            }
            (EmbedBuilder, List<FileAttachment>) GetRolesRemoved()
            {
                var att = new List<FileAttachment>();
                var embed = CreateEmbed(model)
                        .WithTitle("User Roles Removed")
                        .WithColor(Color.Blue);
                info.WithRolesRemoved(embed, att);
                return (embed, att);
            }
            (EmbedBuilder, List<FileAttachment>) GetPermissionsUpdated()
            {
                var att = new List<FileAttachment>();
                var embed = CreateEmbed(model)
                        .WithTitle("User Permissions Updated")
                        .WithColor(Color.Blue);
                info.WithRolesAdded(embed, att);
                info.WithRolesRemoved(embed, att);
                return (embed, att);
            }
        }
        catch (Exception ex)
        {
            var msg = $"Failed to handle event for Guild {model.GuildId}";
            await _errorService.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .AddSerializedAttachment("snapshotInfo.json", info)
                .AddSerializedAttachment("snapshot.before.json", before)
                .AddSerializedAttachment("snapshot.after.json", model));
        }
    }
    #endregion

    private static EmbedBuilder CreateEmbed(GuildMemberSnapshotModel snapshot)
    {
        return new EmbedBuilder()
            .WithDescription(string.Join("\n",
                $"<@{snapshot.UserId}>",
                "```",
                $"Display Name: {snapshot.Nickname ?? snapshot.Username}",
                $"Username: {snapshot.Username}",
                $"Id: {snapshot.UserId}",
                "```"))
            .WithColor(Color.Blue)
            .WithCurrentTimestamp()
            .WithThumbnailUrl(snapshot.AvatarUrl);
    }

    #region Other Member Events
    private async Task DiscordOnUserJoined(SocketGuildUser user)
    {
        if (user == null) return;
        new Thread((roleArg) =>
        {
            if (roleArg is not SocketGuildUser socketGuildUser) return;
            try
            {
                DiscordOnUserJoinedThread(socketGuildUser).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to call {nameof(DiscordOnUserJoinedThread)}");
            }
        }).Start(user);
    }
    private async Task DiscordOnUserLeft(SocketGuild guild, SocketUser user)
    {
        if (guild == null || user == null) return;
        new Thread((threadOptions) =>
        {
            if (threadOptions is not DiscordGuildUserPair pair) return;
            try
            {
                DiscordOnUserLeftThread(pair).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to call {nameof(DiscordOnUserLeftThread)}");
            }
        }).Start(new DiscordGuildUserPair(guild, user));
    }
    private async Task DiscordOnUserBanned(SocketUser user, SocketGuild guild)
    {
        if (guild == null || user == null) return;
        new Thread((threadOptions) =>
        {
            if (threadOptions is not DiscordGuildUserPair pair) return;
            try
            {
                DiscordOnUserBannedThread(pair).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to call {nameof(DiscordOnUserBannedThread)}");
            }
        }).Start(new DiscordGuildUserPair(guild, user));
    }
    private async Task DiscordOnUserUnbanned(SocketUser user, SocketGuild guild)
    {
        if (guild == null || user == null) return;
        new Thread((threadOptions) =>
        {
            if (threadOptions is not DiscordGuildUserPair pair) return;
            try
            {
                DiscordOnUserUnbannedThread(pair).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to call {nameof(DiscordOnUserUnbannedThread)}");
            }
        }).Start(new DiscordGuildUserPair(guild, user));
    }

    private sealed record DiscordGuildUserPair(SocketGuild Guild, SocketUser User);

    private async Task DiscordOnUserJoinedThread(SocketGuildUser user)
    {
        try
        {
            var username = user.Username.Replace("`", "'");
            if (user.DiscriminatorValue > 0) username += $"#{user.Discriminator}";
            var embed = new EmbedBuilder()
                .WithTitle("User Joined")
                .WithDescription(string.Join("\n",
                    user.Mention,
                    "```",
                    $"Display Name: {user.GlobalName.Replace("`", "'")}",
                    $"Username: {username}",
                    $"ID: {user.Id}",
                    "```"
                ))
                .AddField(
                    "Account Age",
                    string.Join("\n",
                            TimeHelper.SinceTimestamp(user.CreatedAt.ToUnixTimeMilliseconds()),
                            $"`{user.CreatedAt}`"
                    ))
                .WithThumbnailUrl(user.GetAvatarUrl())
                .WithCurrentTimestamp()
                .WithColor(Color.Green);

            await _serverLogService.EventHandle(user.Guild.Id, ServerLogEvent.MemberJoin, embed);
        }
        catch (Exception ex)
        {
            var msg = $"Failed to run {nameof(DiscordOnUserJoinedThread)} on \"{user.Username}\" in guild \"{user.Guild.Name}\" (userId={user.Id}, guildId={user.Guild.Id})";

            _log.Error(ex, msg);

            await _errorService.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithUser(user));
        }
    }
    private async Task DiscordOnUserLeftThread(DiscordGuildUserPair options)
    {
        if (options == null) return;

        var (guild, user) = options;

        if (user == null || guild == null) return;
        try
        {
            var userSafe = user.FormatUsername().Replace("`", "'");

            var description = string.Join("\n",
                $"<@{user.Id}>",
                "```",
                $"Display Name: {user.GlobalName.Replace("`", "'")}",
                $"Username: {userSafe}",
                $"ID: {user.Id}",
                "```");
            var userCreatedAt = user.CreatedAt.UtcDateTime.ToString("yyyy/MM/dd HH:mm:ss");
            var accountAge = string.Join("\n",
                TimeHelper.SinceTimestamp(user.CreatedAt.ToUnixTimeMilliseconds()),
                $"`{userCreatedAt}`");

            var embed = new EmbedBuilder()
                .WithTitle("User Left")
                .WithDescription(description)
                .AddField("Account Age", accountAge)
                .WithThumbnailUrl(user.GetAvatarUrl())
                .WithCurrentTimestamp()
                .WithColor(Color.Red);

            await _serverLogService.EventHandle(guild.Id, ServerLogEvent.MemberLeave, embed);
        }
        catch (Exception ex)
        {
            var msg = $"Failed to run {nameof(DiscordOnUserLeftThread)} for user \"{user.FormatUsername()}\" in guild \"{guild.Name}\" (guildId={guild.Id}, userId={user.Id})";
            _log.Error(ex, msg);
            await _errorService.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithUser(user)
                .WithGuild(guild));
        }
    }
    private async Task DiscordOnUserBannedThread(DiscordGuildUserPair options)
    {
        if (options == null) return;

        var (guild, user) = options;

        if (user == null || guild == null) return;
        try
        {
            var userSafe = user.FormatUsername().Replace("`", "'");
            var banDetails = await guild.GetBanAsync(user.Id);
            var embed = new EmbedBuilder()
                .WithTitle("User Banned")
                .WithDescription(string.Join("\n",
                    user.Mention,
                    "```",
                    $"Display Name: {user.GlobalName.Replace("`", "'")}",
                    $"Username: {userSafe}",
                    $"ID: {user.Id}",
                    "```"
                ))
                .AddField("Account Age", string.Join("\n",
                    TimeHelper.SinceTimestamp(user.CreatedAt.ToUnixTimeMilliseconds()),
                    $"`{user.CreatedAt}`"
                ))
                .WithCurrentTimestamp()
                .WithThumbnailUrl(user.GetAvatarUrl())
                .WithColor(Color.Red);
            if (!string.IsNullOrEmpty(banDetails?.Reason?.Trim()))
            {
                embed.AddField("Ban Reason", banDetails.Reason);
            }

            await _serverLogService.EventHandle(guild.Id, ServerLogEvent.MemberBan, embed);
        }
        catch (Exception ex)
        {
            var msg = $"Failed to run {nameof(DiscordOnUserBannedThread)} for user \"{user.FormatUsername()}\" in guild \"{guild.Name}\" (guildId={guild.Id}, userId={user.Id})";
            _log.Error(ex, msg);
            await _errorService.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithUser(user)
                .WithGuild(guild));
        }
    }
    private async Task DiscordOnUserUnbannedThread(DiscordGuildUserPair options)
    {
        if (options == null) return;

        var (guild, user) = options;

        if (user == null || guild == null) return;
        try
        {
            var userSafe = user.FormatUsername().Replace("`", "'");
            var embed = new EmbedBuilder()
                .WithTitle("User Unbanned")
                .WithDescription($"<@{user.Id}>" + string.Join("\n",
                    "```",
                    $"Display Name: {user.GlobalName.Replace("`", "'")}",
                    $"Username: {userSafe}",
                    $"ID: {user.Id}",
                    "```"
                ))
                .AddField("Account Age", string.Join("\n",
                    TimeHelper.SinceTimestamp(user.CreatedAt.ToUnixTimeMilliseconds()),
                    $"`{user.CreatedAt}`"
                ))
                .WithThumbnailUrl(user.GetAvatarUrl())
                .WithColor(Color.Red);

            await _serverLogService.EventHandle(guild.Id, ServerLogEvent.MemberBan, embed);
        }
        catch (Exception ex)
        {
            var msg = $"Failed to run {nameof(DiscordOnUserUnbannedThread)} for user \"{user.FormatUsername()}\" in guild \"{guild.Name}\" (guildId={guild.Id}, userId={user.Id})";
            _log.Error(ex, msg);
            await _errorService.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithUser(user)
                .WithGuild(guild));
        }
    }
    #endregion

    #region Message Events
    private async Task DiscordOnMessageDelete(Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel)
    {
        new Thread((threadOptions) =>
        {
            if (threadOptions is not DiscordOnMessageDeleteParameters pair) return;
            try
            {
                DiscordOnMessageDeleteThread(pair.Message, pair.Channel).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to call {nameof(DiscordOnMessageDeleteThread)}");
            }
        }).Start(new DiscordOnMessageDeleteParameters(message, channel));
    }
    private async void DiscordCacheMessageChangeUpdate(MessageChangeType type, CacheMessageModel current, CacheMessageModel? previous)
    {
        if (type != MessageChangeType.Update) return;
        new Thread((threadOptions) =>
        {
            if (threadOptions is not DiscordCacheMessageChangeUpdateParameters pair) return;
            try
            {
                DiscordCacheMessageChangeUpdateThread(pair.Type, pair.Current, pair.Previous).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to call {nameof(DiscordCacheMessageChangeUpdateThread)}");
            }
        }).Start(new DiscordCacheMessageChangeUpdateParameters(type, current, previous));
    }

    private sealed record DiscordOnMessageDeleteParameters(Cacheable<IMessage, ulong> Message, Cacheable<IMessageChannel, ulong> Channel);
    private sealed record DiscordCacheMessageChangeUpdateParameters(MessageChangeType Type, CacheMessageModel Current, CacheMessageModel? Previous);

    private async Task DiscordOnMessageDeleteThread(Cacheable<IMessage, ulong> m, Cacheable<IMessageChannel, ulong> c)
    {
        var message = await m.GetOrDownloadAsync();
        var channel = await c.GetOrDownloadAsync();

        if (channel is not SocketGuildChannel socketChannel) return;
        try
        {
            if (m.Id == 0)
            {
                _log.Warn($"message.Id is zero?\nhas type of {message.GetType()}");
                return;
            }
            var funkyMessage = await _discordCache.CacheMessageConfig.GetLatest(m.Id);

            var messageContent = message?.Content ?? funkyMessage?.Content ?? "";
            var timestamp =
                message?.CreatedAt.ToUnixTimeSeconds()
                ?? funkyMessage?.CreatedAt.ToUnixTimeSeconds()
                ?? 0;
            if (timestamp == 0)
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
            SocketUser? author = null;
            var authorId = message?.Author.Id ?? funkyMessage?.AuthorId ?? 0;
            if (authorId != 0)
            {
                author = await ExceptionHelper.RetryOnTimedOut(async () => _discord.GetUser(authorId));
            }
            var embed = DiscordHelper.BaseEmbed()
                .WithTitle("Message Deleted")
                .WithDescription($"Deleted in <#{c.Id}> at <t:{timestamp}:F>" + (author == null ? "" : $" from <@{author.Id}> (`{author.Username}`)"))
                .WithColor(Color.Orange);
            if (author != null) embed.WithThumbnailUrl(author.GetAvatarUrl());

            var attachments = new Dictionary<string, string>();
            if (messageContent.Length > 1024)
            {
                embed.AddField("Content", "Attached to this message");
                attachments.Add("content.txt", messageContent);
            }
            else if (messageContent.Length > 0)
            {
                embed.AddField("Content", messageContent);
            }

            if (funkyMessage?.Attachments?.Count > 0)
            {
                var attachmentUrls = string.Join("\n",
                    funkyMessage.Attachments
                    .Select(e => string.IsNullOrEmpty(e.Filename) ? (e.ProxyUrl ?? e.Url) : $"[{e.Filename}]({e.ProxyUrl ?? e.Url})")
                    .Distinct().ToList());
                if (attachmentUrls.Length > 1024)
                {
                    attachments.Add("attachmentUrls.txt", attachmentUrls);
                }
                else if (attachmentUrls.Length > 0)
                {
                    embed.AddField("Attachments", attachmentUrls);
                }
            }
            await _serverLogService.EventHandle(socketChannel.Guild.Id, ServerLogEvent.MessageDelete, embed, attachments);
        }
        catch (Exception ex)
        {
            var msg = string.Join("\n",
                $"Failed run {nameof(DiscordOnMessageDeleteThread)}",
                $"Channel: {socketChannel.Name} ({socketChannel.Id})",
                $"Guild: {socketChannel.Guild.Name} ({socketChannel.Guild.Id})",
                $"MessageId: {message?.Id ?? m.Id}");
            _log.Error(ex, msg);
            await _errorService.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg));
        }
    }
    private async Task DiscordCacheMessageChangeUpdateThread(MessageChangeType type, CacheMessageModel current, CacheMessageModel? previous)
    {
        if (type != MessageChangeType.Update) return;
        try
        {
            var previousContent = previous?.Content ?? "";
            var currentContent = current.Content ?? "";
            if (previousContent == currentContent) return;

            var author = await ExceptionHelper.RetryOnTimedOut<IUser?>(async () => await _discord.GetUserAsync(current.AuthorId));
            if (author == null) return;

            var diffContent = string.Join("\n", SGeneralHelper.GenerateDifference(previousContent ?? "", currentContent ?? ""));

            var embed = DiscordHelper.BaseEmbed()
                .WithTitle("Message Edited")
                .WithDescription(string.Join("\n",
                    $"From: `{author.Username}#{author.Discriminator}`",
                    $"ID: `{current.AuthorId}`"))
                .WithColor(new Color(255, 255, 255))
                .WithUrl($"https://discord.com/channels/{current.GuildId}/{current.ChannelId}/{current.Snowflake}")
                .WithThumbnailUrl(author.GetAvatarUrl());

            var attachments = new Dictionary<string, string>();
            if (diffContent.Contains('`') || diffContent.Length >= 1000)
            {
                embed.AddField("Difference", "Attached as `diff.txt`");
                attachments.Add("diff.txt", diffContent);
            }
            else
            {
                embed.AddField(
                    "Difference",
                    string.Join("\n",
                        "```diff",
                        diffContent,
                        "```"));
            }
            
            await _serverLogService.EventHandle(current.GuildId, ServerLogEvent.MessageEdit, embed, attachments);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to handle MessageChangeUpdate event!!");
            var author = _discord.GetUser(current.AuthorId);
            var guild = _discord.GetGuild(current.GuildId);
            var channel = _discord.GetChannel(current.ChannelId) as IMessageChannel;
            IMessage? msg = null;
            if (channel != null)
                msg = await channel.GetMessageAsync(current.Snowflake);
            await _errorService.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithMessage(msg)
                .WithGuild(guild)
                .WithChannel(channel)
                .WithUser(author));
        }
    }
    #endregion
}