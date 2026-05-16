using System.Collections.Frozen;
using System.Text;
using CSharpFunctionalExtensions;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using NLog;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using XeniaBot.Shared.Helpers;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.ServerLog;
using XeniaDiscord.Data.Models.Snapshot;
using XeniaDiscord.Data.Repositories;
using XeniaDiscord.Data.Models.RolePreserve;

using ServerLogEvent = XeniaDiscord.Data.Models.ServerLog.ServerLogEvent;
using ServerLogRepository = XeniaDiscord.Data.Repositories.ServerLogRepository;
using RolePreserveGuildRepository = XeniaDiscord.Data.Repositories.RolePreserveGuildRepository;

namespace XeniaDiscord.Common.Services;

[XeniaController]
public class RolePreserveService : BaseService
{
    private readonly Logger _log = LogManager.GetLogger("Xenia." + nameof(RolePreserveService));
    private readonly XeniaDbContext _db;
    private readonly ErrorReportService _err;
    private readonly DiscordSocketClient _client;
    private readonly ServerLogRepository _serverLogConfig;
    private readonly RolePreserveUserRepository _userRepository;
    private readonly RolePreserveGuildRepository _guildRepository;
    private readonly ConfigData _configData;
    private readonly ProgramDetails _details;

    public RolePreserveService(IServiceProvider services)
        : base(services)
    {
        _db = services.GetRequiredScopedService<XeniaDbContext>(out var scope);
        _err = services.GetRequiredService<ErrorReportService>();
        _client = services.GetRequiredService<DiscordSocketClient>();
        _serverLogConfig = services.GetRequiredService<ServerLogRepository>();
        _configData = services.GetRequiredService<ConfigData>();
        _details = services.GetRequiredService<ProgramDetails>();
        _userRepository = (scope?.ServiceProvider ?? services).GetRequiredService<RolePreserveUserRepository>();
        _guildRepository = (scope?.ServiceProvider ?? services).GetRequiredService<RolePreserveGuildRepository>();
        var snapshotService = (scope?.ServiceProvider ?? services).GetRequiredService<DiscordSnapshotService>();
        
        if (_details.Platform == XeniaPlatform.Bot)
        {
            _client.UserJoined += ClientOnUserJoined;
            _client.RoleDeleted += ClientOnRoleDeleted;
            snapshotService.GuildMemberUpdated += DiscordSnapshotOnGuildMemberUpdated;
        }
    }

    private Task ClientOnRoleDeleted(
        SocketRole? role)
    {
        if (role == null) return Task.CompletedTask;
        new Thread((roleArg) =>
        {
            if (roleArg is not SocketRole socketRole) return;
            try
            {
                ClientOnRoleDeletedThread(socketRole).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to call {nameof(ClientOnRoleDeletedThread)}");
            }
        })
        {
            Name = $"{nameof(RolePreserveService)}.{nameof(ClientOnRoleDeletedThread)} (roleId={role.Id})"
        }.Start(role);
        return Task.CompletedTask;
    }

    private async Task ClientOnRoleDeletedThread(SocketRole? role)
    {
        if (role == null) return;

        // remove from blacklist & remove from preserved roles
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            var roleIdStr = role.Id.ToString();

            var countUsr = await db.RolePreserveUserRoles
                .AsNoTracking()
                .Where(e => e.RoleId == roleIdStr)
                .ExecuteDeleteAsync();
            var countBlk = await db.RolePreserveBlacklistedRoles
                .AsNoTracking()
                .Where(e => e.RoleId == roleIdStr)
                .ExecuteDeleteAsync();
            
            await db.SaveChangesAsync();
            await trans.CommitAsync();
            
            if (countUsr > 0)
            {
                _log.Trace($"Deleted {countUsr} records in {RolePreserveUserRoleModel.TableName} for RoleId={role.Id}");
            }
            if (countBlk > 0)
            {
                _log.Trace($"Deleted {countBlk} records in {RolePreserveBlacklistedRoleModel.TableName} for RoleId={role.Id}");
            }
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            _log.Error(ex, $"Failed to delete records for RoleId={role.Id}");
        }
    }

    private async Task DiscordSnapshotOnGuildMemberUpdated(
        GuildMemberSnapshotModel? before,
        GuildMemberSnapshotModel model)
    {
        // skip if roles are the same, or if the user wasn't actually updated
        var rolesMatch = model.RolesMatch(before);
        if (!rolesMatch ||
            (model.SnapshotSource != GuildMemberSnapshotSource.MemberUpdate &&
            model.SnapshotSource != GuildMemberSnapshotSource.RoleDelete))
        {
            _log.Trace($"Skipping DB Update (guildId={model.GuildId}, userId={model.UserId}, username={model.Username}, rolesMatch={rolesMatch}, modelSnapshotSource={model.SnapshotSource})");
            return;
        }

        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            await _userRepository.UpdateSnapshot(db, model);
            
            await db.SaveChangesAsync();
            await trans.CommitAsync();
            _log.Trace($"Successfully handled event (GuildId={model.GuildId}, UserId={model.UserId}, Username={model.Username}, SnapshotSource={model.SnapshotSource})");
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            _log.Error(ex, $"Failed to handle event for user {model.Username} ({model.UserId}) in guild {model.GuildId}");
            // TODO submit to ErrorReportService
        }
    }
    
    private async Task ClientOnUserJoined(SocketGuildUser user)
    {
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            if (!await _guildRepository.IsEnabled(db, user.Guild.Id))
            {
                _log.Trace($"Skipping Role Preserve is disabled (guildId={user.Guild.Id}, userId={user.Id}, username={user.Username})");
                await trans.RollbackAsync();
                return;
            }
            if (!await _userRepository.HasAny(db, user.Guild.Id, user.Id))
            {
                _log.Trace($"Skipping since there are no records in {RolePreserveUserModel.TableName} (guildId={user.Guild.Id}, userId={user.Id}, username={user.Username})");
                await trans.RollbackAsync();
                return;
            }
            var roleIds = await _userRepository.FindRolesForUser(db, user.Guild.Id, user.Id);
            var blacklist = await _guildRepository.GetBlacklistRolesForGuild(db, user.Guild.Id);
            
            var ourHighestRoleEnumerable = user.Guild.CurrentUser.Roles.OrderByDescending(v => v.Position);
            var ourHighestRolePos = ourHighestRoleEnumerable.FirstOrDefault()?.Position ?? int.MinValue;

            var audit = new RolePreserveAuditModel()
            {
                GuildId = user.Guild.Id.ToString(),
                TargetUserId = user.Id.ToString(),
                Action = RolePreserveAuditAction.AppliedRoles
            };
            
            var success = new List<ulong>();
            var fail = new List<ApplyFailure>();
            foreach (var item in roleIds)
            {
                var roleId = item;
                if (roleId == user.Guild.EveryoneRole.Id) continue;
                if (blacklist.Any(e => e.RoleId == item.ToString()))
                {
                    audit.AppliedRoles.Add(new RolePreserveAuditAppliedRoleModel()
                    {
                        RolePreserveAuditId = audit.Id,
                        RoleId = item.ToString(),
                        Action = RolePreserveAuditAppliedRoleAction.SkippedBlacklisted
                    });
                    continue;
                }
                try
                {
                    var snapshot = await db.GuildRoleSnapshots
                        .AsNoTracking()
                        .OrderByDescending(e => e.RecordCreatedAt)
                        .FirstOrDefaultAsync(e => e.RoleId == roleId.ToString());
                    var existingRole = await ExceptionHelper.RetryOnTimedOut(async () => await user.Guild.GetRoleAsync(roleId));
                    // continue, since the role doesn't exist anymore
                    if (existingRole == null)
                    {
                        audit.AppliedRoles.Add(new RolePreserveAuditAppliedRoleModel()
                        {
                            RolePreserveAuditId = audit.Id,
                            RoleId = item.ToString(),
                            Action = RolePreserveAuditAppliedRoleAction.SkippedRoleDoesNotExist
                        });
                        continue;
                    }
                    if (existingRole.Position > ourHighestRolePos)
                    {
                        audit.AppliedRoles.Add(new RolePreserveAuditAppliedRoleModel()
                        {
                            RolePreserveAuditId = audit.Id,
                            RoleId = item.ToString(),
                            Action = RolePreserveAuditAppliedRoleAction.FailureMissingPermissionsHierarchy
                        });
                        fail.Add(new (item, snapshot));
                        continue;
                    }
                    await user.AddRoleAsync(roleId);
                    success.Add(roleId);
                    audit.AppliedRoles.Add(new RolePreserveAuditAppliedRoleModel()
                    {
                        RolePreserveAuditId = audit.Id,
                        RoleId = item.ToString(),
                        Action = RolePreserveAuditAppliedRoleAction.SuccessGrant
                    });
                }
                catch (Exception ex)
                {
                    _log.Warn(ex, $"Failed to grant Role {item} to User \"{user.Username}#{user.Discriminator}\" ({user.Id}) in Guild \"{user.Guild.Name}\" ({user.Guild.Id})");
                    fail.Add(new(item, null));
                    audit.AppliedRoles.Add(new RolePreserveAuditAppliedRoleModel()
                    {
                        RolePreserveAuditId = audit.Id,
                        RoleId = item.ToString(),
                        Action = RolePreserveAuditAppliedRoleAction.FailureUnknown,
                        ExceptionText = ex.ToString()
                    });
                }
            }

            _log.Trace($"Operation complete (userId={user.Id}, username={user.Username}, success={success.Count}, fail={fail.Count})");
            if (success.Count < 1)
            {
                _log.Trace($"No roles were restored? (guildId={user.Guild.Id}, userId={user.Id}, username={user.Username})");
            }
            if (fail.Count > 0)
            {
                try
                {
                    await SendFailureNotification(user, success.ToFrozenSet(), fail);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"Failed to send failure notification for User \"{user.Username}#{user.Discriminator}\" ({user.Id}) in Guild \"{user.Guild.Name}\" ({user.Guild.Id})");
                }
            }

            await db.AddAsync(audit);
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            var msg = $"Failed to restore roles for User \"{user.Username}#{user.Discriminator}\" ({user.Id}) in Guild \"{user.Guild.Name}\" ({user.Guild.Id})";
            _log.Error(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithUser(user)
                .WithGuild(user.Guild));
        }
    }
    private sealed record ApplyFailure(ulong RoleId, GuildRoleSnapshotModel? Snapshot);
    
    #region Send Failure Notification
    private async Task SendFailureNotification(
        SocketGuildUser user,
        IReadOnlyCollection<ulong> success,
        IReadOnlyCollection<ApplyFailure> fail,
        RolePreserveAuditModel? auditModel = null)
    {
        if (fail.Count < 1) return;
        IReadOnlyCollection<ServerLogChannelModel> targetLogChannels;
        try
        {
            targetLogChannels = await _serverLogConfig.GetChannelsForGuild(user.Guild.Id, [ServerLogEvent.MemberJoin], new()
            {
                IgnoreDisabledGuilds = true
            });
            if (targetLogChannels.Count < 1) return;
        }
        catch (Exception ex)
        {
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes($"Failed to get Server Log Channel models with event {ServerLogEvent.MemberJoin} for Guild \"{user.Guild.Name}\" ({user.Guild.Id})")
                .WithUser(user)
                .WithGuild(user.Guild));
            return;
        }

        var successCount = success.Count.ToString("n0");
        var successPlural = success.Count == 1 ? "" : "s";
        var embed = new EmbedBuilder()
            .WithDescription($"Added {successCount} role{successPlural} successfully.")
            .WithTitle("Role Preserve - User Joined - " + user.Username)
            .WithFooter($"User Id: {user.Id}")
            .WithColor(new Color(255, 255, 255))
            .WithCurrentTimestamp();
        if (auditModel != null && _configData.HasDashboard)
        {
            embed.WithUrl($"{_configData.DashboardUrl}/RolePreserve/Audit/Details?Id={auditModel.Id}");
        }
        var failCount = fail.Count.ToString("n0");
        if (success.Count == 0)
        {
            embed.WithDescription($"- {Emotes.Warning} Failed to give user *any* roles");
            if (fail.Count > 0)
            {
                embed.Description += $" ({failCount})";
            }
        }
        else if (fail.Count > 0)
        {
            var failPlural = fail.Count == 1 ? "" : "s";
            if (fail.Count > 0) embed.Description += $"\n- Failed to add {failCount} role{failPlural}.";
        }

        const string failFilename = "roles.txt";
        var attachments = new List<FileAttachment>();
        var failureFieldContent = GetFailEmbedContent(fail)
            .TapError(err =>
            {
                if (err != GetFailEmbedContentError.AttachFailures) return;
                var failAttachmentContent = GetFailAttachment(fail);
                attachments.Add(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(failAttachmentContent)), failFilename));
            })
            .Finally(r =>
            {
                var sb = new StringBuilder();
                if (r.IsFailure)
                {
                    switch (r.Error)
                    {
                        case GetFailEmbedContentError.AttachFailures:
                            sb.Append(Emotes.Warning);
                            sb.AppendFormat(" Too many roles failed! It's been attached as `{0}`", failFilename);
                            break;
                        default:
                            sb.Append(Emotes.Warning);
                            sb.Append(" Unknown error: ");
                            sb.Append(r.Error);
                            break;
                    }
                }
                else
                {
                    sb.Append(r.Value);
                }
                
                return sb.ToString();
            });
        embed.AddField("Failed Roles", failureFieldContent);
        foreach (var serverLogChannel in targetLogChannels)
        {
            await SendFailureNotificationToChannel(user, embed, attachments, serverLogChannel);
        }
    }
    private enum GetFailEmbedContentError
    {
        AttachFailures
    }
    private static Result<string, GetFailEmbedContentError> GetFailEmbedContent(
        IReadOnlyCollection<ApplyFailure> items)
    {
        /* determined with the following code:
        const int max = 1024;
        int lineSize = string.Format("- <@&{0}>\n", ulong.MaxValue).Length; // expected to be 26
        int iterCount = Convert.ToInt32(Math.Floor(max / (float)(lineSize)));
         */
        const int maxCountSafe = 37;
        if (items.Count > maxCountSafe) return GetFailEmbedContentError.AttachFailures;

        var sb = new StringBuilder();
        var count = items.Count;
        for (var i = 0; i < count; i++)
        {
            var item = items.ElementAt(i);
            sb.Append("- <@&");
            sb.Append(item.RoleId);
            sb.Append('>');
            if (i < count - 1)
            {
                sb.AppendLine();
            }
        }
        // added just to be safe
        if (sb.Length >= 1024) return GetFailEmbedContentError.AttachFailures;
        return sb.ToString();
    }
    private static string GetFailAttachment(
        IReadOnlyCollection<ApplyFailure> items)
    {
        var sb = new StringBuilder();
        foreach (var item in items)
        {
            sb.Append(item.RoleId);
            sb.Append(" - ");
            sb.Append(item.Snapshot?.Name);
            sb.AppendLine();
        }
        return sb.ToString();
    }
    private async Task SendFailureNotificationToChannel(
        SocketGuildUser user,
        EmbedBuilder embed,
        List<FileAttachment> attachments,
        ServerLogChannelModel serverLogChannel)
    {
        SocketTextChannel? textChannel;
        try
        {
            textChannel = user.Guild.GetTextChannel(serverLogChannel.GetChannelId())
                          ?? throw new InvalidOperationException($"Channel {serverLogChannel.ChannelId} does not exist (GetTextChannel returned null)");
        }
        catch (Exception ex)
        {
            _log.Warn(ex, $"Could not get channel {serverLogChannel.ChannelId} in Guild \"{user.Guild}\" ({user.Guild.Id}) from ServerLogChannel with Id={serverLogChannel.Id}");
            return;
        }
        try
        {
            if (attachments.Count > 0)
            {
                await textChannel.SendFilesAsync(attachments, embed: embed.Build());
            }
            else
            {
                await textChannel.SendMessageAsync(embed: embed.Build());
            }
        }
        catch (Exception ex)
        {
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes($"Failed to send message in channel \"{textChannel.Name}\" ({textChannel.Id}) in Guild \"{user.Guild.Name}\" ({user.Guild.Id}) for user \"{user.Username}#{user.Discriminator}\" ({user.Id})")
                .WithUser(user)
                .WithGuild(user.Guild)
                .WithChannel(textChannel)
                .AddSerializedAttachment("serverLogChannel.json", serverLogChannel));
        }
    }
    #endregion

    public override Task OnReadyDelay()
    {
        // skip on web panel
        if (_details.Platform != XeniaPlatform.Bot) return Task.CompletedTask;
        if (!_configData.RefreshRolePreserveOnStart)
        {
            _log.Info($"Not going to run {nameof(PreserveAll)} since {nameof(_configData.RefreshRolePreserveOnStart)} is set to false");
            return Task.CompletedTask;
        }
        new Thread(PreserveAllThread)
        {
            Name = $"{nameof(RolePreserveService)}.{nameof(PreserveAllThread)}"
        }.Start();
        return Task.CompletedTask;
    }
    private void PreserveAllThread()
    {
        try
        {
            PreserveAll().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to run {nameof(PreserveAll)}");
        }
    }

    public async Task PreserveAll(DateTime? startedAt = null)
    {
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            var start = startedAt ?? DateTime.UtcNow;
            foreach (var guild in _client.Guilds)
            {
                await PerformGuild(db, guild.Id, start: start);
            }
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            const string msg = "Failed to preserve all guilds";
            _log.Error(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg));
        }
    }

    private async Task PerformGuild(XeniaDbContext db, ulong guildId, DateTime? start = null)
    {
        SocketGuild? guild;
        try
        {
            guild = _client.GetGuild(guildId);
            if (guild == null) return;
        }
        catch (Exception ex)
        {
            var msg = $"Failed to get Guild {guildId} for role preservation";
            _log.Warn(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg));
            return;
        }
        try
        {
            var result = await UseLatestSnapshotsForGuild(db, guild, start: start);
            if (result.IsFailure) throw new InvalidOperationException(result.Error);
        }
        catch (Exception ex)
        {
            var msg = $"Failed to preserve roles for Guild \"{guild.Name}\" ({guild.Id})";
            _log.Warn(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithGuild(guild));
        }
    }
    
    public async Task PreserveGuild(SocketGuild guild, DateTime? startedAt = null)
    {
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            var result = await UseLatestSnapshotsForGuild(db, guild, start: startedAt);
            if (result.IsFailure) throw new InvalidOperationException(result.Error);
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
        var memberCount = guild.Users.Count.ToString("n0");
        _log.Info($"Preserved all roles in Guild \"{guild.Name}\" ({guild.Id}), which archived {memberCount} members.");
    }
    
    public async Task<UnitResult<string>> UseLatestSnapshotsForGuild(
        XeniaDbContext db,
        ulong guildId,
        DateTime? start = null)
    {
        var guild = _client.GetGuild(guildId);
        if (guild == null) return $"Guild does not exist: `{guildId}`";

        return await UseLatestSnapshotsForGuild(db, guild, start: start);
    }

    public async Task<UnitResult<string>> UseLatestSnapshotsForGuild(ulong guildId, DateTime? start = null)
    {
        var guild = _client.GetGuild(guildId);
        if (guild == null) return $"Guild does not exist: `{guildId}`";
        
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            var result = await UseLatestSnapshotsForGuild(db, guild, start: start);
            if (result.IsFailure)
            {
                await trans.RollbackAsync();
                return result;
            }
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
        return UnitResult.Success<string>();
    }

    public async Task<UnitResult<string>> UseLatestSnapshotsForGuild(
        XeniaDbContext db,
        SocketGuild guild,
        DateTime? start = null)
    {
        var userRoles = guild.Users
            .Select(e => new
            {
                Key = e.Id,
                Value = e.Roles.Select(r => r.Id).Distinct().ToArray()
            })
            .ToDictionary(e => e.Key, e => e.Value);
        var startValue = start ?? DateTime.UtcNow;
        foreach (var (userId, roleIds) in userRoles)
        {
            await _userRepository.InsertOrUpdate(db, guild.Id, userId, roleIds, start: startValue);
        }
        return UnitResult.Success<string>();
    }
}