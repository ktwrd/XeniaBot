using System.Globalization;
using CSharpFunctionalExtensions;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.Cache;
using XeniaDiscord.Data.Models.RolePreserve;
using XeniaDiscord.Data.Models.Snapshot;
using XeniaDiscord.Data.Repositories;
using RolePreserveGuildRepository = XeniaDiscord.Data.Repositories.RolePreserveGuildRepository;

namespace XeniaDiscord.Common.Services;

[XeniaController]
public class RolePreserveService : BaseService
{
    private readonly Logger _log = LogManager.GetLogger("Xenia." + nameof(RolePreserveService));
    private readonly ErrorReportService _err;
    private readonly DiscordShardedClient _client;
    private readonly RolePreserveUserRepository _userRepository;
    private readonly RolePreserveGuildRepository _guildRepository;
    private readonly RolePreserveLogService _rolePreserveLogService;
    private readonly ConfigData _configData;
    private readonly ProgramDetails _details;
    private readonly IDbContextFactory<XeniaDbContext> _dbContextFactory;
    private readonly IMapper<IRole, GuildRoleSnapshotModel> _roleToSnapshotMapper;
    private readonly IMapper<GuildRoleSnapshotModel, GuildRoleCacheModel> _roleSnapshotToCacheMapper;

    public RolePreserveService(IServiceProvider services)
        : base(services)
    {
        _err = services.GetRequiredService<ErrorReportService>();
        _client = services.GetRequiredService<DiscordShardedClient>();
        _configData = services.GetRequiredService<ConfigData>();
        _details = services.GetRequiredService<ProgramDetails>();
        _userRepository = services.GetRequiredService<RolePreserveUserRepository>();
        _guildRepository = services.GetRequiredService<RolePreserveGuildRepository>();
        var snapshotService = services.GetRequiredService<DiscordSnapshotService>();
        _rolePreserveLogService = services.GetRequiredService<RolePreserveLogService>();
        _dbContextFactory = services.GetRequiredService<IDbContextFactory<XeniaDbContext>>();
        _roleToSnapshotMapper = services.GetRequiredService<IMapper<IRole, GuildRoleSnapshotModel>>();
        _roleSnapshotToCacheMapper = services.GetRequiredService<IMapper<GuildRoleSnapshotModel, GuildRoleCacheModel>>();

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
        await using var db = await _dbContextFactory.CreateDbContextAsync();
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

        await using var db = await _dbContextFactory.CreateDbContextAsync();
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

    private async Task EnsureRoleInCache(
        XeniaDbContext db,
        DateTime now,
        IGuildUser user,
        ulong roleId)
    {
        var guildIdStr = user.GuildId.ToString(CultureInfo.InvariantCulture);
        var roleIdStr = roleId.ToString(CultureInfo.InvariantCulture);
        var existingRoleCache = await db.GuildRoleCache.FindAsync(roleIdStr);
        if (existingRoleCache == null)
        {
            var existingRole = user.Guild.Roles.All(r => r.Id != roleId)
                ? null
                : await ExceptionHelper.RetryOnTimedOut(async () => await user.Guild.GetRoleAsync(roleId));
            var latestRoleSnapshot = await db.GuildRoleSnapshots
                .Where(e => e.RoleId == roleIdStr)
                .OrderByDescending(e => e.RecordCreatedAt)
                .FirstOrDefaultAsync();

            if (latestRoleSnapshot == null)
            {
                latestRoleSnapshot = new GuildRoleSnapshotModel()
                {
                    RecordCreatedAt = now,
                    SnapshotSource = GuildRoleSnapshotSource.Unknown,
                    GuildId = guildIdStr,
                    RoleId = roleIdStr,
                    Name = null,
                    CreatedAt = SnowflakeUtils.FromSnowflake(roleId).UtcDateTime,
                    Position = -1,
                    Flags = RoleFlags.None,
                    IsManaged = false,
                    IsMentionable = false,
                    IsHoisted = false
                };
                if (existingRole != null)
                {
                    latestRoleSnapshot = _roleToSnapshotMapper.Map(existingRole);
                    latestRoleSnapshot.RecordCreatedAt = now;
                }
                await db.AddAsync(latestRoleSnapshot);

                existingRoleCache = _roleSnapshotToCacheMapper.Map(latestRoleSnapshot);
                if (existingRole == null)
                {
                    existingRoleCache.IsDeleted = true;
                    existingRoleCache.DeletedAt = latestRoleSnapshot.RecordCreatedAt;
                }
                await db.AddAsync(existingRoleCache);
            }
            else
            {
                existingRoleCache = _roleSnapshotToCacheMapper.Map(latestRoleSnapshot);
            }
            existingRoleCache.RecordUpdatedAt = now;
            existingRoleCache.RecordCreatedAt = now;
            existingRoleCache.SnapshotId = latestRoleSnapshot.Id;
            await db.AddAsync(existingRoleCache);
        }
    }
    
    private async Task ClientOnUserJoined(SocketGuildUser user)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        await using var trans = await db.Database.BeginTransactionAsync();
        var auditId = Guid.NewGuid();
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
                Id = auditId,
                GuildId = user.Guild.Id.ToString(),
                TargetUserId = user.Id.ToString(),
                Action = RolePreserveAuditAction.AppliedRoles
            };

            foreach (var item in roleIds)
            {
                var roleId = item;
                if (roleId == user.Guild.EveryoneRole.Id) continue;
                await EnsureRoleInCache(db, audit.RecordCreatedAt, user, roleId);
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
                    var existingRole = user.Guild.Roles.All(r => r.Id != roleId)
                        ? null
                        : await ExceptionHelper.RetryOnTimedOut(async () => await user.Guild.GetRoleAsync(roleId));
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
                        continue;
                    }
                    await user.AddRoleAsync(roleId, new RequestOptions
                    {
                        AuditLogReason = $"Role Preserve (Audit ID: {audit.Id})"
                    });
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
                    audit.AppliedRoles.Add(new RolePreserveAuditAppliedRoleModel()
                    {
                        RolePreserveAuditId = audit.Id,
                        RoleId = item.ToString(),
                        Action = RolePreserveAuditAppliedRoleAction.FailureUnknown,
                        ExceptionText = ex.ToString()
                    });
                }
            }

            var failCount = audit.AppliedRoles.Count(e => e.IsActionFailure());
            var successCount = audit.AppliedRoles.Count(e => e.IsActionSuccess());
            
            _log.Trace($"Operation complete (userId={user.Id}, username={user.Username}, success={successCount}, fail={failCount})");
            if (successCount < 1)
            {
                _log.Trace($"No roles were restored? (guildId={user.Guild.Id}, userId={user.Id}, username={user.Username})");
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

        RolePreserveAuditModel? tmpAuditModel = null;
        try
        {
            tmpAuditModel = await db.RolePreserveAudit
                .AsNoTracking()
                .Include(e => e.AppliedRoles)
                .ThenInclude(e => e.Role)
                .Include(e => e.ReferencedRoles)
                .ThenInclude(e => e.Role)
                .FirstAsync(e => e.Id == auditId);
            await _rolePreserveLogService.SendAuditNotification(user, tmpAuditModel);
        }
        catch (Exception ex)
        {
            var msg = $"Failed to send failure notification for RolePreserveAudit.ID={auditId} for User \"{user.Username}#{user.Discriminator}\" in Guild \"{user.Guild.Name}\" (userId: {user.Id}, guildId: {user.Guild.Id})";
            _log.Error(ex, msg);
            await _err.Submit(
                new ErrorReportBuilder()
                    .WithException(ex)
                    .WithNotes(msg)
                    .AddSerializedAttachment("rolePreserveAudit.json", tmpAuditModel));
        }
    }

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
        await using var db = await _dbContextFactory.CreateDbContextAsync();
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
            guild = ExceptionHelper.RetryOnTimedOut(() => _client.GetGuild(guildId));
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
        await using var db = await _dbContextFactory.CreateDbContextAsync();
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
        var guild = ExceptionHelper.RetryOnTimedOut(() => _client.GetGuild(guildId));
        if (guild == null) return $"Guild does not exist: `{guildId}`";

        return await UseLatestSnapshotsForGuild(db, guild, start: start);
    }

    public async Task<UnitResult<string>> UseLatestSnapshotsForGuild(ulong guildId, DateTime? start = null)
    {
        var guild = ExceptionHelper.RetryOnTimedOut(() => _client.GetGuild(guildId));
        if (guild == null) return $"Guild does not exist: `{guildId}`";

        await using var db = await _dbContextFactory.CreateDbContextAsync();
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