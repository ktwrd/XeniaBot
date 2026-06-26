using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using JetBrains.Annotations;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.Snapshot;
using XeniaDiscord.Data.Repositories;

namespace XeniaDiscord.Common.Services;

[UsedImplicitly]
public class DiscordSnapshotService : BaseService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly XeniaDbContext _db;
    private readonly DiscordCacheService _cacheService;
    private readonly GuildCacheRepository _guildCacheRepository;
    private readonly IDbContextFactory<XeniaDbContext> _dbFactory;
    private readonly IMapper<IRole, GuildRoleSnapshotModel> _roleMapper;
    private readonly IMapper<IGuildUser, GuildMemberSnapshotModel> _guildMemberMapper;

    private readonly ErrorReportService _err;
    public DiscordSnapshotService(IServiceProvider services) : base(services)
    {
        _db = services.GetRequiredScopedService<XeniaDbContext>(out var _);
        var client = services.GetRequiredService<DiscordSocketClient>();
        _cacheService = services.GetRequiredService<DiscordCacheService>();
        _guildCacheRepository = services.GetRequiredService<GuildCacheRepository>();
        _dbFactory = services.GetRequiredService<IDbContextFactory<XeniaDbContext>>();
        _roleMapper = services.GetRequiredService<IMapper<IRole, GuildRoleSnapshotModel>>();
        _guildMemberMapper = services.GetRequiredService<IMapper<IGuildUser, GuildMemberSnapshotModel>>();

        _err = services.GetRequiredService<ErrorReportService>();

        var programDetails = services.GetRequiredService<ProgramDetails>();
        if (programDetails.Platform == XeniaPlatform.Bot)
        {
            client.JoinedGuild += OnGuildJoined;
            client.GuildUpdated += OnGuildUpdated;
            client.LeftGuild += OnGuildLeft;

            client.UserJoined += OnGuildMemberJoined;
            client.GuildMemberUpdated += OnGuildMemberUpdated;
            client.RoleCreated += OnGuildRoleCreated;
            client.RoleUpdated += OnGuildRoleUpdated;
            client.RoleDeleted += OnGuildRoleDeleted;
        }
    }

    /// <summary>
    /// Invoked when a member has been updated.
    /// </summary>
    [UsedImplicitly]
    public event DiscordSnapshotComparisonDelegate<GuildMemberSnapshotModel>? GuildMemberUpdated;

    /// <summary>
    /// Invoked when a role has been updated, created, or deleted.
    /// </summary>
    [UsedImplicitly]
    public event DiscordSnapshotComparisonDelegate<GuildRoleSnapshotModel>? GuildRoleUpdated;

    /// <summary>
    /// Invoked when a role has been deleted.
    /// </summary>
    [UsedImplicitly]
    public event DiscordSnapshotComparisonDelegate<GuildRoleSnapshotModel>? GuildRoleDeleted;

    /// <summary>
    /// Invoked when the bot joins a guild, or when it's been updated.
    /// </summary>
    [UsedImplicitly]
    public event DiscordSnapshotComparisonDelegate<GuildSnapshotModel>? GuildUpdated;

    private Task OnGuildJoined(SocketGuild guild)
    {
        new Thread(() =>
        {
            try
            {
                ProcessGuild(guild, DiscordSnapshotSource.JoinedGuild).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to process Guild \"{guild.Name}\" ({guild.Id})");
            }
        }).Start();
        return Task.CompletedTask;
    }

    private Task OnGuildUpdated(SocketGuild _, SocketGuild guild)
    {
        new Thread(() =>
        {
            try
            {
                ProcessGuild(guild, DiscordSnapshotSource.GuildUpdated, skipRoles: true, skipMembers: true).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to process Guild \"{guild.Name}\" ({guild.Id})");
            }
        }).Start();
        return Task.CompletedTask;
    }

    private Task OnGuildLeft(SocketGuild guild)
    {
        new Thread(() =>
        {
            try
            {
                ProcessGuild(guild, DiscordSnapshotSource.LeftGuild).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to process Guild \"{guild.Name}\" ({guild.Id})");
            }
        }).Start();
        return Task.CompletedTask;
    }

    private Task OnGuildMemberJoined(SocketGuildUser member)
    {
        new Thread(() =>
        {
            try
            {
                ProcessGuildMember(member, GuildMemberSnapshotSource.MemberJoin).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to process {member} ({member.Id}) in guild {member.Guild.Name} ({member.Guild.Id})");
            }
        }).Start();
        return Task.CompletedTask;
    }

    private Task OnGuildMemberUpdated(
        Cacheable<SocketGuildUser, ulong> before,
        SocketGuildUser member)
    {
        new Thread(() =>
        {
            try
            {
                ProcessGuildMember(member, GuildMemberSnapshotSource.MemberUpdate).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to process {member} ({member.Id}) in guild {member.Guild.Name} ({member.Guild.Id})");
            }
        }).Start();
        return Task.CompletedTask;
    }

    private Task OnGuildRoleCreated(SocketRole role)
    {
        new Thread(() =>
        {
            try
            {
                ProcessRole(GuildRoleSnapshotSource.RoleCreate, null, role).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to process role {role.Name} ({role.Id}) in guild {role.Guild.Name} ({role.Guild.Id})");
            }
        }).Start();
        return Task.CompletedTask;
    }
    private Task OnGuildRoleUpdated(SocketRole? roleBefore, SocketRole role)
    {
        new Thread(() =>
        {
            try
            {
                ProcessRole(GuildRoleSnapshotSource.RoleEdit, roleBefore, role).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to process role {role.Name} ({role.Id}) in guild {role.Guild.Name} ({role.Guild.Id})");
            }
        }).Start();
        return Task.CompletedTask;
    }
    private Task OnGuildRoleDeleted(SocketRole role)
    {
        _log.Trace($"Id={role?.Id},name={role?.Name},guildId={role?.Guild.Id},guildName={role?.Guild.Name}");
        if (role == null) return Task.CompletedTask;
        new Thread(() =>
        {
            try
            {
                ProcessRole(GuildRoleSnapshotSource.RoleDelete, null, role).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to process role {role.Name} ({role.Id}) in guild {role.Guild.Name} ({role.Guild.Id})");
            }
        }).Start();
        return Task.CompletedTask;
    }

    private async Task ProcessGuild(SocketGuild guild, DiscordSnapshotSource source,
        bool skipRoles = false, bool skipMembers = false)
    {
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;
            await UpdateGuild(db, guild, now, source,
                skipRoles: skipRoles,
                skipMembers: skipMembers);
            await _cacheService.UpdateGuild(db, guild, now,
                includeMembers: source != DiscordSnapshotSource.LeftGuild && !skipMembers);
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            _log.Error(ex, $"Failed to pull data for Guild \"{guild.Name}\" (guildId={guild.Id}, source={source}, skipRoles={skipRoles}, skipMembers={skipMembers})");
            return;
        }

        var guildId = guild.Id.ToString();
        var latestEvent = await db.GuildSnapshotEvent
            .AsNoTracking()
            .Where(e => e.GuildId == guildId && e.Source == source)
            .OrderByDescending(e => e.Timestamp)
            .Select(e => new { e.BeforeId, e.CurrentId })
            .FirstOrDefaultAsync();
        if (latestEvent == null) return;
        
        var before = await db.GuildSnapshots.AsNoTracking().FirstOrDefaultAsync(e => e.RecordId == latestEvent.BeforeId);
        var after = await db.GuildSnapshots.AsNoTracking().FirstOrDefaultAsync(e => e.RecordId == latestEvent.CurrentId);
        
        if (after == null) return;
        
        GuildUpdated?.Invoke(before, after);
    }

    private readonly List<GuildMemberUpdateQueueItem> _guildMemberQueue = [];
    private readonly SemaphoreSlim _guildMemberQueueLock = new(1, 1);

    public sealed record GuildMemberUpdateQueueItem(
        DateTimeOffset Timestamp,
        SocketGuildUser Dto,
        GuildMemberSnapshotSource Source);
    public sealed record ReduceGuildMemberQueueResult(
        IReadOnlyCollection<GuildMemberSnapshotModel> SnapshotsToAdd,
        IReadOnlyCollection<GuildMemberSnapshotEventModel> SnapshotEventsToAdd);
    // TODO create unit tests for this so if it's implemented properly before integrating it with everything else
    public async Task<ReduceGuildMemberQueueResult> ReduceGuildMemberQueue(
        IEnumerable<GuildMemberUpdateQueueItem> enumerable)
    {
        await using var dbo = await _dbFactory.CreateDbContextAsync();
        var snapshots = new List<GuildMemberSnapshotModel>();
        var events = new List<GuildMemberSnapshotEventModel>();
        foreach (var queue in enumerable
                     .GroupBy(e => new { UserId = e.Dto.Id, GuildId = e.Dto.Guild.Id }))
        {
            var queueArr = queue.OrderBy(e => e.Timestamp).ToArray();
            var local = new List<GuildMemberUpdateQueueItem>();
            foreach (var item in queueArr)
            {
                local.Add(item);
                if (local.Count < 2) continue;
                var lastDelta = item.Timestamp - local[0].Timestamp;
                if (lastDelta <= TimeSpan.FromSeconds(5)) continue;
                await TriggerLocal(dbo, local);
                local = [];
                /*
                var modelBefore = await GetBeforeGuildMemberSnapshotModel(
                    queue.Key.GuildId, queue.Key.UserId,
                    local[0].Timestamp);
                if (modelBefore == null)
                {
                    modelBefore = _guildMemberMapper.Map(local[^1].Dto);
                    modelBefore.SnapshotSource = local[^1].Source;
                }

                var modelAfter = _guildMemberMapper.Map(item.Dto);
                modelAfter.SnapshotSource = item.Source;
                events.Add(new GuildMemberSnapshotEventModel()
                {
                    Timestamp = item.Timestamp.UtcDateTime,
                    Before = modelBefore,
                    Current = modelAfter
                });
                local = [];
                */
            }

            if (local.Count > 0) await TriggerLocal(dbo, local);
            local = null!;
        }

        return new ReduceGuildMemberQueueResult(snapshots, events);
        async Task TriggerLocal(XeniaDbContext db, List<GuildMemberUpdateQueueItem> localList)
        {
            var oldest = localList.First();
            var latest = localList.Last();
            var modelBefore = await GetBeforeGuildMemberSnapshotModel(
                db,
                oldest.Dto.Guild.Id, oldest.Dto.Id,
                oldest.Timestamp);

            var modelAfter = _guildMemberMapper.Map(latest.Dto);
            modelAfter.RecordCreatedAt = latest.Timestamp.UtcDateTime;
            modelAfter.SnapshotSource = latest.Source;
            snapshots.Add(modelAfter);
            if (oldest == latest)
            {
                events.Add(new GuildMemberSnapshotEventModel()
                {
                    Timestamp = modelAfter.RecordCreatedAt,
                    GuildId = modelAfter.GuildId,
                    UserId = modelAfter.UserId,
                    Source = DiscordSnapshotSource.MemberUpdated,
                    WhatChanged = FindWhatChanged(modelBefore, modelAfter),
                    BeforeId = modelBefore?.RecordId,
                    AfterId = modelAfter.RecordId
                });
                return;
            }

            if (modelBefore == null)
            {
                modelBefore = _guildMemberMapper.Map(oldest.Dto);
                modelBefore.RecordCreatedAt = oldest.Timestamp.UtcDateTime;
                modelBefore.SnapshotSource = oldest.Source;
                snapshots.Add(modelBefore);
            }

            events.Add(new GuildMemberSnapshotEventModel()
            {
                Timestamp = modelAfter.RecordCreatedAt,
                GuildId = modelAfter.GuildId,
                UserId = modelAfter.UserId,
                Source = DiscordSnapshotSource.MemberUpdated,
                WhatChanged = FindWhatChanged(modelBefore, modelAfter),
                BeforeId = modelBefore.RecordId,
                AfterId = modelAfter.RecordId
            });
        }
    }

    // TODO add more/proper event handling
    // TODO add logic to save queue to disk or database (or use real software like rabbitmq lol)
    public async Task ProcessQueue(ReduceGuildMemberQueueResult queue)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            await db.AddRangeAsync(queue.SnapshotsToAdd);
            await db.AddRangeAsync(queue.SnapshotEventsToAdd);

            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            throw new InvalidOperationException($"Failed to process queue!", ex);
        }

        foreach (var @event in queue.SnapshotEventsToAdd)
        {
            var eventModel = await db.GuildMemberSnapshotEvents
                .Include(e => e.Before)
                .ThenInclude(e => e.Permissions)
                .Include(e => e.Before)
                .ThenInclude(e => e.Roles)
                .ThenInclude(e => e.GuildRoleSnapshot)
                .Include(e => e.Current)
                .ThenInclude(e => e.Permissions)
                .Include(e => e.Current)
                .ThenInclude(e => e.Roles)
                .ThenInclude(e => e.GuildRoleSnapshot)
                .FirstOrDefaultAsync(e => e.Id == @event.Id);
            GuildMemberUpdated?.Invoke(eventModel?.Before ?? @event.Before, eventModel?.Current ?? @event.Current);
        }
    }
    private static GuildMemberSnapshotEventWhatChanged FindWhatChanged(
            GuildMemberSnapshotModel? before,
            GuildMemberSnapshotModel after)
    {
        if (before == null) return default;
        GuildMemberSnapshotEventWhatChanged flags = default;
        
        if (!string.Equals(before.Username, after.Username, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(before.Discriminator, after.Discriminator, StringComparison.OrdinalIgnoreCase))
            flags |= GuildMemberSnapshotEventWhatChanged.Username;
        
        if (!string.Equals(before.Nickname, after.Nickname, StringComparison.OrdinalIgnoreCase))
            flags |= GuildMemberSnapshotEventWhatChanged.Nickname;
        
        if (before.IsSelfDeafened != after.IsSelfDeafened ||
            before.IsSelfMuted != after.IsSelfMuted ||
            before.IsSuppressed != after.IsSuppressed ||
            before.IsDeafened != after.IsDeafened ||
            before.IsMuted != after.IsMuted ||
            before.IsStreaming != after.IsStreaming ||
            before.GetVoiceChannelId() != after.GetVoiceChannelId())
            flags |= GuildMemberSnapshotEventWhatChanged.VoiceStatus;
        
        if (!string.Equals(before.GuildAvatarId, after.GuildAvatarId, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(before.AvatarUrl, after.AvatarUrl, StringComparison.OrdinalIgnoreCase))
            flags |= GuildMemberSnapshotEventWhatChanged.Avatar;
        
        var rolesBefore = before.Roles.Select(e => e.RoleId).Distinct().ToHashSet();
        var rolesAfter = after.Roles.Select(e => e.RoleId).Distinct().ToHashSet();
        if (rolesBefore.Any(b => !rolesAfter.Contains(b)) ||
            rolesAfter.Any(a => !rolesBefore.Contains(a)))
            flags |= GuildMemberSnapshotEventWhatChanged.Roles;

        var permissionsBefore = before.Permissions.Select(e => e.GetValue()).ToArray();
        var permissionsAfter = after.Permissions.Select(e => e.GetValue()).ToArray();
        if (permissionsBefore.Any(b => !permissionsAfter.Contains(b)) ||
            permissionsAfter.Any(a => !permissionsBefore.Contains(a)))
            flags |= GuildMemberSnapshotEventWhatChanged.Permissions;

        if (before.TimedOutUntil?.Ticks != after.TimedOutUntil?.Ticks ||
            before.IsPending != after.IsPending)
            flags |= GuildMemberSnapshotEventWhatChanged.Moderation;

        return flags;
    }

    private async Task<GuildMemberSnapshotModel?> GetBeforeGuildMemberSnapshotModel(
        ulong guildId,
        ulong userId,
        DateTimeOffset? before)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await GetBeforeGuildMemberSnapshotModel(db, guildId, userId, before);
    }
    private async Task<GuildMemberSnapshotModel?> GetBeforeGuildMemberSnapshotModel(
        XeniaDbContext db,
        ulong guildId,
        ulong userId,
        DateTimeOffset? before)
    {
        var guildIdStr = guildId.ToString();
        var userIdStr = userId.ToString();
        var q = db.GuildMemberSnapshots.AsNoTracking()
            .Include(e => e.Roles)
            .Include(e => e.Permissions)
            .Where(e => e.GuildId == guildIdStr && e.UserId == userIdStr);
        if (before != null)
        {
            var time = before.Value.UtcDateTime;
            q = q.Where(e => e.RecordCreatedAt < time);
        }
        return await q.OrderByDescending(e => e.RecordCreatedAt).FirstOrDefaultAsync();
    }

    private async Task ProcessGuildMember(SocketGuildUser socketMemberAfter, GuildMemberSnapshotSource source)
    {
        var userIdStr = socketMemberAfter.Id.ToString();
        var guildIdStr = socketMemberAfter.Guild.Id.ToString();
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        GuildMemberSnapshotModel? modelBefore = null;
        try
        {
            modelBefore = await db.GuildMemberSnapshots.AsNoTracking()
                .Include(e => e.Roles)
                .Include(e => e.Permissions)
                .OrderByDescending(e => e.RecordCreatedAt)
                .FirstOrDefaultAsync(e => e.GuildId == guildIdStr && e.UserId == userIdStr);
        }
        catch (Exception ex)
        {
            var msg = $"Failed to find Guild Member Snapshot (userId={userIdStr},guildId={guildIdStr})";
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithUser(socketMemberAfter));
        }

        GuildMemberSnapshotModel model;
        try
        {
            model = _guildMemberMapper.Map(socketMemberAfter);
            model.SnapshotSource = source;
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            var msg = $"Failed to map {socketMemberAfter.GetType()} (userId: {userIdStr}, guildId: {guildIdStr})";
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithUser(socketMemberAfter));
            return;
        }
        try
        {
            await db.AddAsync(model);
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            const string msg = "Failed to add record into database";
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithUser(socketMemberAfter)
                .AddSerializedAttachment("model.json", model));
            return;
        }
        GuildMemberUpdated?.Invoke(modelBefore, model);
    }

    private async Task ProcessRole(
        GuildRoleSnapshotSource source,
        SocketRole? roleBefore,
        SocketRole role)
    {
        var roleIdStr = role.Id.ToString();
        var guildIdStr = role.Guild.Id.ToString();
        var now = DateTime.UtcNow;
        GuildRoleSnapshotModel? modelBefore = null;
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            modelBefore = await db.GuildRoleSnapshots.AsNoTracking()
                .OrderByDescending(e => e.RecordCreatedAt)
                .FirstOrDefaultAsync(e => e.GuildId == guildIdStr && e.RoleId == roleIdStr);
        }
        catch (Exception ex)
        {
            var msg = $"Failed to find Guild Role Snapshot (roleId={roleIdStr}, guildId={guildIdStr}, source={source})";
            _log.Error(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithRole(role));
        }
        if (modelBefore == null && roleBefore != null)
        {
            try
            {
                modelBefore = _roleMapper.Map(roleBefore);
                var c = modelBefore.RecordCreatedAt - TimeSpan.FromSeconds(1);
                if (c < modelBefore.CreatedAt) c = modelBefore.CreatedAt;
                modelBefore.RecordCreatedAt = c;
                await db.AddAsync(modelBefore);
            }
            catch (Exception ex)
            {
                var msg = $"Failed to map \"before\" state of Role \"{role.Name}\" in Guild \"{role.Guild.Name}\" (roleId={role.Id}, guildId={role.Guild.Id}, source={source})";
                _log.Error(ex, msg);
                await _err.Submit(new ErrorReportBuilder()
                    .WithException(ex)
                    .WithNotes(msg)
                    .WithRole(roleBefore));
            }
        }

        GuildRoleSnapshotModel model;
        try
        {
            model = _roleMapper.Map(role);
            model.SnapshotSource = source;
            model.RecordCreatedAt = now;
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();

            var msg = $"Failed to map Role \"{role.Name}\" in Guild \"{role.Guild.Name}\" (roleId={role.Id}, guildId={role.Guild.Id}, source={source})";
            _log.Error(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithRole(role));
            return;
        }
        try
        {
            bool? isDeletedValue = source switch
            {
                GuildRoleSnapshotSource.RoleDelete => true,
                GuildRoleSnapshotSource.RoleCreate => false,
                GuildRoleSnapshotSource.RoleEdit => false,
                _ => null
            };
            await db.AddAsync(model);
            await _guildCacheRepository.UpdateRoleCache(db, model, isDeleted: isDeletedValue, now: now);
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            var msg = $"Failed to add record into database (roleId={role.Id}, guildId={role.Guild.Id}, source={source})";
            _log.Error(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithRole(role)
                .AddSerializedAttachment("model.json", model));
            return;
        }
        try
        {
            GuildRoleUpdated?.Invoke(modelBefore, model);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to invoke {nameof(GuildRoleUpdated)} (roleId={role.Id}, guildId={role.Guild.Id}, source={source})");
        }
        if (source == GuildRoleSnapshotSource.RoleDelete)
        {
            try
            {
                GuildRoleDeleted?.Invoke(modelBefore, model);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to invoke {nameof(GuildRoleDeleted)} (roleId={role.Id}, guildId={role.Guild.Id}, source={source})");
            }
        }
    }

    public async Task UpdateGuild(
        XeniaDbContext db,
        IGuild guild,
        DateTime now,
        DiscordSnapshotSource source,
        bool skipRoles = false,
        bool skipMembers = false)
    {
        if (!skipRoles)
        {
            await UpdateGuildRoles(db, guild, now, source);
        }
        if (!skipMembers)
        {
            await UpdateGuildMembers(db, guild, now, source);
        }

        GuildSnapshotModel guildSnapshot;
        try
        {
            guildSnapshot = new GuildSnapshotModel()
            {
                RecordCreatedAt = now,
                SnapshotSource = source
            };
            guildSnapshot.Update(guild);
            var guildSnapshotBefore = await db.GuildSnapshots
                .AsNoTracking()
                .OrderByDescending(e => e.RecordCreatedAt)
                .Where(e => e.GuildId == guildSnapshot.GuildId)
                .FirstOrDefaultAsync();
            await db.GuildSnapshots.AddAsync(guildSnapshot);
            if (guildSnapshotBefore != null)
            {
                await db.GuildSnapshotEvent.AddAsync(new GuildSnapshotEventModel
                {
                    Timestamp = now,
                    GuildId = guildSnapshot.GuildId,
                    Source = source,
                    BeforeId = guildSnapshotBefore?.RecordId,
                    CurrentId = guildSnapshot.RecordId,
                });
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to insert GuildSnapshot for \"{guild.Name}\" (guildId={guild.Id}, source={source})", ex);
        }
    }

    public async Task UpdateGuildRoles(
        XeniaDbContext db,
        IGuild guild,
        DateTime now,
        DiscordSnapshotSource source)
    {
        var roles = new List<GuildRoleSnapshotModel>();
        foreach (var role in guild.Roles)
        {
            try
            {
                var mapped = _roleMapper.Map(role);
                mapped.RecordCreatedAt = now;
                mapped.SnapshotSource = source switch
                {
                    DiscordSnapshotSource.RoleCreated => GuildRoleSnapshotSource.RoleCreate,
                    DiscordSnapshotSource.RoleUpdated => GuildRoleSnapshotSource.RoleEdit,
                    DiscordSnapshotSource.RoleDeleted => GuildRoleSnapshotSource.RoleDelete,
                    _ => GuildRoleSnapshotSource.Unknown
                };
                roles.Add(mapped);
            }
            catch (Exception ex)
            {
                _log.Warn(ex, $"Failed to map role \"{role.Name}\" in Guild \"{guild.Name}\" (guildId={guild.Id}, roleId={role.Id}, source={source})");
            }
        }
        try
        {
            await db.AddRangeAsync(roles);
            bool? isDeletedValue = source switch
            {
                DiscordSnapshotSource.RoleDeleted => true,
                DiscordSnapshotSource.RoleCreated => false,
                DiscordSnapshotSource.RoleUpdated => false,
                _ => null
            };
            foreach (var role in roles)
            {
                await _guildCacheRepository.UpdateRoleCache(db, role, isDeleted: isDeletedValue);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save roles for Guild \"{guild.Name}\" (guildId={guild.Id}, source={source})", ex);
        }
    }

    private async Task UpdateGuildMembers(
        XeniaDbContext db,
        IGuild guild,
        DateTime now,
        DiscordSnapshotSource source)
    {
        var members = new List<GuildMemberSnapshotModel>();
        foreach (var member in await guild.GetUsersAsync())
        {
            try
            {
                var mapped = _guildMemberMapper.Map(member);
                mapped.RecordCreatedAt = now;
                mapped.SnapshotSource = source switch
                {
                    DiscordSnapshotSource.MemberJoined => GuildMemberSnapshotSource.MemberJoin,
                    DiscordSnapshotSource.MemberUpdated => GuildMemberSnapshotSource.MemberUpdate,
                    DiscordSnapshotSource.RoleDeleted => GuildMemberSnapshotSource.RoleDelete,
                    DiscordSnapshotSource.JoinedGuild => GuildMemberSnapshotSource.GuildJoined,
                    _ => GuildMemberSnapshotSource.Unknown
                };
                members.Add(mapped);
            }
            catch (Exception ex)
            {
                _log.Warn(ex, $"Failed to map member \"{member.GlobalName}\" ({member.Username}) in Guild \"{guild.Name}\" (guildId={guild.Id}, userId={member.Id}, source={source})");
            }
        }
        try
        {
            await db.AddRangeAsync(members);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save members for Guild \"{guild.Name}\" (guildId={guild.Id}, source={source})", ex);
        }
    }
}

public delegate Task DiscordSnapshotComparisonDelegate<in TModel>(TModel? before, TModel model);
public delegate Task DiscordSnapshotComparisonSourceDelegate<in TModel>(DiscordSnapshotEventSource source, TModel? before, TModel model);
public delegate Task DiscordSnapshotSourceDelegate<in TModel>(DiscordSnapshotEventSource source, ulong id, TModel? snapshot);

public enum DiscordSnapshotEventSource
{
    Create,
    Edit,
    Delete
}