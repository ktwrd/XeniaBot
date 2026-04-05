using CSharpFunctionalExtensions;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
using XeniaDiscord.Data.Repositories;
using DiscordCacheService = XeniaBot.Core.Services.Wrappers.DiscordCacheService;

namespace XeniaBot.Core.Services.BotAdditions;

[XeniaController]
public class ServerLogService : BaseService
{
    private readonly Logger _log = LogManager.GetLogger("Xenia." + nameof(ServerLogService));
    private readonly ServerLogRepository _serverLogRepo;
    private readonly DiscordSocketClient _discord;
    private readonly DiscordCacheService _discordCache;
    private readonly DiscordSnapshotService _discordSnapshot;
    private readonly ErrorReportService _errorService;
    private readonly XeniaDbContext _db;
    public ServerLogService(IServiceProvider services)
        : base(services)
    {
        _serverLogRepo = services.GetRequiredService<ServerLogRepository>();
        _discord = services.GetRequiredService<DiscordSocketClient>();
        _discordCache = services.GetRequiredService<DiscordCacheService>();
        _discordSnapshot = services.GetRequiredService<DiscordSnapshotService>();
        _errorService = services.GetRequiredService<ErrorReportService>();
        _db = services.GetRequiredScopedService<XeniaDbContext>(out var _);
    }

    public override Task InitializeAsync()
    {
        _discord.UserJoined += Event_UserJoined;
        _discord.UserLeft += Event_UserLeave;
        _discord.UserBanned += Event_UserBan;
        _discord.UserUnbanned += Event_UserBanRemove;

        _discord.MessageDeleted += Event_MessageDelete;
        _discordCache.MessageChange += DiscordCacheMessageChangeUpdate;

        _discordSnapshot.GuildMemberUpdated += DiscordSnapshotGuildMemberUpdate;
        _discordSnapshot.GuildRoleUpdated += DiscordSnapshotGuildRoleUpdate;

        return Task.CompletedTask;
    }

    private async Task DiscordSnapshotGuildRoleUpdate(
        DiscordSnapshotEventSource source,
        GuildRoleSnapshotModel? before,
        GuildRoleSnapshotModel model)
    {
        var guildIdStr = model.GuildId;
        if (before == null && source == DiscordSnapshotEventSource.Edit)
        {
            _log.Trace($"Event skipped. No before state for Edit source (guildId={model.GuildId}, roleId={model.RoleId}, recordId={model.Id})");
            return;
        }
        await using var db = _db.CreateSession();
        DiscordSnapshotRoleUpdateInfo? info = null;
        try
        {
            // disabled in guild, ignore
            if (!await db.ServerLogGuilds.AnyAsync(e => e.GuildId == guildIdStr && e.Enabled)) return;

            var permissionsAddedList = new List<GuildPermission>();
            var permissionsRemovedList = new List<GuildPermission>();
            
            if (source == DiscordSnapshotEventSource.Delete)
            {
                permissionsRemovedList.AddRange(model.Permissions.Select(e => e.GetValue()));
            }
            else if (source == DiscordSnapshotEventSource.Create)
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
                Source = source
            };
            if (!info.Any) return;
            
            _log.Trace($"Handling event (source={source}, guildId={model.GuildId}, roleId={model.RoleId}, recordId={model.Id})");

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var embed = new EmbedBuilder()
                .WithDescription(string.Join("\n",
                    $"<@&{model.RoleId}>",
                    "Name: `" + model.Name?.Replace("`", "'") + "`"))
                .WithFooter("ID: " + model.RoleId)
                .WithColor(Color.Blue)
                .WithCurrentTimestamp();
            var attachments = new List<FileAttachment>();

            switch (source)
            {
                case DiscordSnapshotEventSource.Create:
                    embed.WithTitle("Role Created");
                    info.WithInfo(embed, attachments);
                    info.WithPermissionsUpdated(embed, attachments);

                    await EventHandle(
                        model.GetGuildId(),
                        ServerLogEvent.RoleCreate,
                        [embed],
                        attachments);
                    break;
                case DiscordSnapshotEventSource.Edit:
                    embed.WithTitle("Role Updated")
                         .WithDescription(string.Join("\n",
                        $"<@&{model.RoleId}> was updated <t:{now}:R> (name: {model.Name?.Replace("`", "'")})",
                        "Name: `" + model.Name?.Replace("`", "'") + "`"));
                    info.WithInfo(embed, attachments);
                    info.WithPermissionsUpdated(embed, attachments);

                    await EventHandle(
                        model.GetGuildId(),
                        ServerLogEvent.RoleCreate,
                        [embed],
                        attachments);
                    break;
                case DiscordSnapshotEventSource.Delete:
                    embed.WithTitle("Role Deleted");
                    info.WithPermissionsUpdated(embed, attachments);

                    await EventHandle(
                        model.GetGuildId(),
                        ServerLogEvent.RoleCreate,
                        [embed],
                        attachments);
                    break;
            }
        }
        catch (Exception ex)
        {
            var msg = $"Failed to handle event for Guild {model.GuildId} and Role {model.RoleId} (SnapshotId={model.Id})";
            await _errorService.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .AddSerializedAttachment("snapshotInfo.json", info)
                .AddSerializedAttachment("snapshot.before.json", before)
                .AddSerializedAttachment("snapshot.after.json", model));
        }
    }

    private async Task DiscordSnapshotGuildMemberUpdate(
        GuildMemberSnapshotModel? before,
        GuildMemberSnapshotModel model)
    {
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
                    await EventHandle(model.GetGuildId(), ServerLogEvent.MemberPermissionsUpdated, [permEmbed], permAtt);
                }
            }
            else if (events.Contains(ServerLogEvent.Fallback) || events.Contains(ServerLogEvent.MemberUpdated))
            {
                await SendAs(GetCombined(), ServerLogEvent.MemberUpdated);
            }

            async Task SendAs((EmbedBuilder, List<FileAttachment>) tuple, ServerLogEvent @event)
            {
                var (embed, att) = tuple;
                await EventHandle(model.GetGuildId(), @event, [embed], att);
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

    internal Task EventHandle(ulong serverId, ServerLogEvent @event, EmbedBuilder embed, Dictionary<string, string>? attachments = null)
    {
        return EventHandle(serverId, @event, [embed], attachments);
    }
    internal Task EventHandle(ulong serverId, ServerLogEvent @event, EmbedBuilder[] embeds, Dictionary<string, string>? attachments = null)
    {
        return EventHandle(
            serverId,
            @event,
            embeds,
            attachments?.Select(e => new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(e.Value)), e.Key))
            .ToList());
    }
    internal async Task EventHandle(ulong serverId, ServerLogEvent @event, EmbedBuilder[] embeds, List<FileAttachment>? attachments = null)
    {
        var targetChannels = await _serverLogRepo.GetChannelsForGuild(serverId, [@event, ServerLogEvent.Fallback]);
        var guild = _discord.GetGuild(serverId);
        if (guild == null) return;
        foreach (var channel in targetChannels)
        {
            try
            {
                await ProcessForModel(channel);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to send event {@event} to channel {channel.ChannelId} in guild \"{guild.Name}\" ({guild.Id})");
            }
        }
        async Task ProcessForModel(ServerLogChannelModel channelModel)
        {
            var logChannel = guild.GetTextChannel(channelModel.GetChannelId());
            if (logChannel == null) return;
            
            await ExceptionHelper.RetryOnTimedOut(async () =>
            {
                await ProcessForModelInner(logChannel);
            });
        }
        async Task ProcessForModelInner(SocketTextChannel logChannel)
        {
            try
            {
                if (attachments?.Count < 1)
                {
                    await logChannel.SendMessageAsync(embeds: embeds.Select(e => e.Build()).ToArray());
                    return;
                }

                await logChannel.SendFilesAsync(attachments, embeds: embeds.Select(e => e.Build()).ToArray());
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Missing Access") || ex.Message.Contains("50001") || ex.Message.Contains("50013"))
                {
                    try
                    {
                        await guild.Owner.SendMessageAsync(
                            string.Join(
                                "\n",
                                "Heya!", "",
                                $"Xenia does not have access to send log events in a channel in the server `{guild.Name}`, which you own.",
                                "",
                                "In order for the logging feature to work, make sure that Xenia has access to the following permissions.",
                                "- View Channel",
                                "- Send Messages",
                                "- Embed Links",
                                "", $"Channel affected: https://discord.com/channels/{guild.Id}/{logChannel.Id}"
                            ));
                    }
                    catch (Exception exx)
                    {
                        _log.Error(exx, $"Failed to DM owner of guild \"{guild.Name}\" ({guild.Id}), {guild.Owner.Username} ({guild.OwnerId}) about not having the correct permissions in channel \"{logChannel.Name}\" ({logChannel.Id})");
                    }
                }
                else
                {
                    throw;
                }
            }
        }
    }

    #region User Events
    private async Task Event_UserJoined(SocketGuildUser user)
    {
        try
        {
            var userSafe = user.Username.Replace("`", "\\`");
            var embed = new EmbedBuilder()
                .WithTitle("User Joined")
                .WithDescription(string.Join("\n",
                    user.Mention,
                    "```",
                    $"Display Name: {user.GlobalName.Replace("`", "'")}",
                    $"Username: {userSafe}#{user.Discriminator}",
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
                .WithColor(Color.Green);

            await EventHandle(user.Guild.Id, ServerLogEvent.MemberJoin, embed);
        }
        catch (Exception ex)
        {
            await _errorService.ReportException(
                ex,
                $"Failed to run Event_UserJoined on {user} ({user.Id}) in guild {user.Guild.Name} ({user.Guild.Id})");
        }
    }
    private async Task Event_UserLeave(SocketGuild guild, SocketUser user)
    {
        try
        {
            var userSafe = user.Username.Replace("`", "\\`");

            var description = string.Join("\n",
                $"<@{user.Id}>",
                "```",
                $"Display Name: {user.GlobalName.Replace("`", "'")}",
                $"Username: {userSafe}#{user.Discriminator}",
                $"ID: {user.Id}",
                "```");

            var accountAge = string.Join("\n",
                TimeHelper.SinceTimestamp(user.CreatedAt.ToUnixTimeMilliseconds()),
                $"`{user.CreatedAt}`");

            var embed = new EmbedBuilder()
                .WithTitle("User Left")
                .WithDescription(description)
                .AddField("Account Age", accountAge)
                .WithThumbnailUrl(user.GetAvatarUrl())
                .WithColor(Color.Red);

            await EventHandle(guild.Id, ServerLogEvent.MemberLeave, embed);
        }
        catch (Exception ex)
        {
            await _errorService.ReportException(
                ex,
                $"Failed to run Event_UserLeave on {user} ({user.Id}) in guild {guild.Name} ({guild.Id})");
        }
    }

    private async Task Event_UserBan(SocketUser user, SocketGuild guild)
    {
        try
        {
            var userSafe = user.Username.Replace("`", "\\`");
            var banDetails = await guild.GetBanAsync(user.Id);
            var embed = new EmbedBuilder()
                .WithTitle("User Banned")
                .WithDescription(string.Join("\n",
                    user.Mention,
                    "```",
                    $"Display Name: {user.GlobalName.Replace("`", "'")}",
                    $"Username: {userSafe}#{user.Discriminator}",
                    $"ID: {user.Id}",
                    "```"
                ))
                .AddField("Account Age", string.Join("\n",
                    TimeHelper.SinceTimestamp(user.CreatedAt.ToUnixTimeMilliseconds()),
                    $"`{user.CreatedAt}`"
                ))
                .WithThumbnailUrl(user.GetAvatarUrl())
                .WithColor(Color.Red);
            if (!string.IsNullOrEmpty(banDetails?.Reason?.Trim()))
            {
                embed.AddField("Ban Reason", banDetails.Reason);
            }

            await EventHandle(guild.Id, ServerLogEvent.MemberBan, embed);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to run");
            await _errorService.ReportException(
                ex,
                $"Failed run ServerLogService.Event_UserBan.\nUser: {user} ({user.Id})\nGuild: {guild.Name} ({guild.Id})");
        }
    }
    private async Task Event_UserBanRemove(SocketUser user, SocketGuild guild)
    {
        try
        {
            var userSafe = user.Username.Replace("`", "'");
            var embed = new EmbedBuilder()
                .WithTitle("User Unbanned")
                .WithDescription($"<@{user.Id}>" + string.Join("\n",
                    "```",
                    $"Display Name: {user.GlobalName.Replace("`", "'")}",
                    $"Username: {userSafe}#{user.Discriminator}",
                    $"ID: {user.Id}",
                    "```"
                ))
                .AddField("Account Age", string.Join("\n",
                    TimeHelper.SinceTimestamp(user.CreatedAt.ToUnixTimeMilliseconds()),
                    $"`{user.CreatedAt}`"
                ))
                .WithThumbnailUrl(user.GetAvatarUrl())
                .WithColor(Color.Red);

            await EventHandle(guild.Id, ServerLogEvent.MemberBan, embed);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to run");
            await _errorService.ReportException(
                ex,
                $"Failed run ServerLogService.Event_UserBanRemove.\nUser: {user} ({user.Id})\nGuild: {guild.Name} ({guild.Id})");
        }
    }
    #endregion
    
    #region Message Events

    private async Task Event_MessageDelete(Cacheable<IMessage, ulong> m, Cacheable<IMessageChannel, ulong> c)
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
                author = _discord.GetUser(authorId);
            }
            var embed = DiscordHelper.BaseEmbed()
                .WithTitle("Message Deleted")
                .WithDescription($"Deleted in <#{c.Id}> at <t:{timestamp}:F>" + (author == null ? "" : $" from <@{author.Id}> (`{author.Username}`)"))
                .WithColor(Color.Orange);
            if (author != null)
                embed.WithThumbnailUrl(author.GetAvatarUrl());

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
            await EventHandle(socketChannel.Guild.Id, ServerLogEvent.MessageDelete, embed, attachments);
        }
        catch (Exception ex)
        {
            var msg = string.Join("\n",
                "Failed run ServerLogService.Event_MessageDelete.",
                $"ChannelId: {socketChannel.Id}",
                $"Guild: {socketChannel.Guild.Id} ({socketChannel.Guild.Name})",
                $"MessageId: {message?.Id ?? m.Id}");
            _log.Error(ex, msg);
            await _errorService.ReportException(
                ex,
                msg);
        }
    }
    private async void DiscordCacheMessageChangeUpdate(MessageChangeType type, CacheMessageModel current, CacheMessageModel? previous)
    {
        try
        {
            if (type != MessageChangeType.Update)
                return;

            var previousContent = previous?.Content ?? "";
            var currentContent = current.Content ?? "";
            if (previousContent == currentContent)
                return;

            var author = await ExceptionHelper.RetryOnTimedOut<IUser?>(async () => await _discord.GetUserAsync(current.AuthorId));
            if (author == null)
                return;

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
            if (diffContent.Length >= 1000)
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
            
            await EventHandle(current.GuildId, ServerLogEvent.MessageEdit, embed, attachments);
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
            await DiscordHelper.ReportError(ex, author, guild, channel, msg);
        }
    }
    #endregion
}