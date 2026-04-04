using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Helpers;
using XeniaBot.Data.Models.Archival;
using XeniaBot.DiscordCache.Models;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;
using XeniaDiscord.Common.Services;
using XeniaDiscord.Data.Models.ServerLog;
using XeniaDiscord.Data.Models.Snapshot;
using XeniaDiscord.Data.Repositories;
using NLog;

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
    public ServerLogService(IServiceProvider services)
        : base(services)
    {
        _serverLogRepo = services.GetRequiredService<ServerLogRepository>();
        _discord = services.GetRequiredService<DiscordSocketClient>();
        _discordCache = services.GetRequiredService<DiscordCacheService>();
        _discordSnapshot = services.GetRequiredService<DiscordSnapshotService>();
        _errorService = services.GetRequiredService<ErrorReportService>();
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

        return Task.CompletedTask;
    }

    private async Task DiscordSnapshotGuildMemberUpdate(
        GuildMemberSnapshotModel? before,
        GuildMemberSnapshotModel model)
    {
        if (before == null)
        {
            _log.Trace($"Event. No before state (guildId={model.GuildId}, userId={model.UserId})");
            return;
        }
        DiscordSnapshotMemberUpdateInfo? info = null;
        try
        {
            var rolesAdded = new List<ulong>();
            var rolesRemoved = new List<ulong>();

            rolesAdded.AddRange(model.Roles
                .Where(a => !before.Roles.Any(b => b.RoleId == a.RoleId))
                .Select(e => e.GetRoleId()));

            rolesRemoved.AddRange(before.Roles
                .Where(b => !model.Roles.Any(a => a.RoleId == b.RoleId))
                .Select(e => e.GetRoleId()));

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
                PermissionsRemoved = permissionsRemovedList
            };

            // TODO send message in log channel with updated roles/permissions/nickname
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

    public class DiscordSnapshotMemberUpdateInfo
    {
        public required IReadOnlyCollection<ulong> RolesAdded { get; init; }
        public required IReadOnlyCollection<ulong> RolesRemoved { get; init; }
        public required IReadOnlyCollection<GuildPermission> PermissionsAdded { get; init; }
        public required IReadOnlyCollection<GuildPermission> PermissionsRemoved { get; init; }
    }

    internal async Task EventHandle(ulong serverId, ServerLogEvent @event, EmbedBuilder embed, Dictionary<string, string>? attachments = null)
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
                var attachmentList = new List<FileAttachment>();
                foreach (var pair in attachments ?? [])
                {
                    attachmentList.Add(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(pair.Value)), pair.Key));
                }
                if (attachmentList.Count < 1)
                {
                    await logChannel.SendMessageAsync(embed: embed.Build());
                    return;
                }

                await logChannel.SendFilesAsync(attachmentList, embed: embed.Build());
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