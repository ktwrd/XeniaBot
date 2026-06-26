using Discord;
using Microsoft.EntityFrameworkCore;
using NLog;
using XeniaDiscord.Data.Models.RolePreserve;

namespace XeniaDiscord.Data.Repositories;

public class RolePreserveGuildRepository
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly XeniaDbContext _db;

    public RolePreserveGuildRepository(IServiceProvider services)
    {
        _db = services.GetRequiredScopedService<XeniaDbContext>(out var scope);
    }
    
    public async Task<RolePreserveGuildModel?> GetAsync(ulong guildId, QueryOptions? options = null)
    {
        await using var db = _db.CreateSession();
        return await GetAsync(db, guildId, options);
    }
    
    public async Task<RolePreserveGuildModel?> GetAsync(
        XeniaDbContext db,
        ulong guildId,
        QueryOptions? options = null)
    {
        var guildIdStr = guildId.ToString();
        return await Apply(db.RolePreserveGuilds, options)
              .AsNoTracking()
              .FirstOrDefaultAsync(e => e.GuildId == guildIdStr);
    }

    public Task<bool> IsEnabled(ulong guildId)
        => IsEnabled(_db, guildId);

    public async Task<bool> IsEnabled(
        XeniaDbContext db,
        ulong guildId)
    {
        var guildIdStr = guildId.ToString();
        return await db.RolePreserveGuilds
            .AnyAsync(e => e.GuildId == guildIdStr && e.Enabled);
    }

    public async Task InsertOrUpdate(
        XeniaDbContext db,
        RolePreserveGuildModel model)
    {
        if (await db.RolePreserveGuilds.AnyAsync(e => e.GuildId == model.GuildId))
        {
            await db.RolePreserveGuilds
                .Where(e => e.GuildId == model.GuildId)
                .ExecuteUpdateAsync(e => e
                .SetProperty(p => p.Enabled, model.Enabled));
            _log.Trace($"Updated Record (GuildId={model.GuildId}, Enabled={model.Enabled}");
        }
        else
        {
            await db.RolePreserveGuilds.AddAsync(model);
            _log.Trace($"Created Record (GuildId={model.GuildId}, Enabled={model.Enabled}");
        }
    }

    public async Task EnableAsync(XeniaDbContext db, ulong guildId, bool enable, IUser? doneByUser = null)
    {
        var guildIdStr = guildId.ToString();
        // return if we're not really updating anything
        if (await db.RolePreserveGuilds.AnyAsync(e => e.GuildId == guildIdStr && e.Enabled == enable))
        {
            return;
        }
        if (await db.RolePreserveGuilds.AnyAsync(e => e.GuildId == guildIdStr))
        {
            await db.RolePreserveGuilds
                .Where(e => e.GuildId == guildIdStr)
                .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.Enabled, enable));
            _log.Trace($"Updated Record (GuildId={guildIdStr}, Enabled={enable}");
        }
        else
        {
            await db.RolePreserveGuilds.AddAsync(new RolePreserveGuildModel
            {
                GuildId = guildIdStr,
                Enabled = enable
            });
            _log.Trace($"Created Record (GuildId={guildIdStr}, Enabled={enable}");
        }
        var userIdStr = doneByUser?.Id.ToString();
        await db.RolePreserveAudit.AddAsync(new RolePreserveAuditModel()
        {
            GuildId = guildIdStr,
            Action = enable ? RolePreserveAuditAction.Enable : RolePreserveAuditAction.Disable,
            UserId = userIdStr
        });
    }

    public async Task<List<RolePreserveBlacklistedRoleModel>> GetBlacklistedRoles(
        XeniaDbContext db)
    {
        return await db.RolePreserveBlacklistedRoles
            .AsNoTracking()
            .OrderBy(e => e.GuildId)
            .ThenBy(e => e.RoleId)
            .ToListAsync();
    }

    public Task<List<RolePreserveBlacklistedRoleModel>> GetBlacklistRolesForGuild(
        XeniaDbContext db,
        IGuild guild)
        => GetBlacklistRolesForGuild(db, guild.Id);
    public async Task<List<RolePreserveBlacklistedRoleModel>> GetBlacklistRolesForGuild(
        XeniaDbContext db,
        ulong guildId)
    {
        var guildIdStr = guildId.ToString();
        return await db.RolePreserveBlacklistedRoles
            .AsNoTracking()
            .Where(e => e.GuildId == guildIdStr)
            .OrderBy(e => e.RoleId)
            .ToListAsync();
    }

    public async Task<RoleBlacklistAddResult> RoleBlacklistAdd(
        XeniaDbContext db,
        IGuild guild,
        IRole role,
        IGuildUser? doneByUser = null)
    {
        if (guild.Id != role.Guild.Id)
        {
            return RoleBlacklistAddResult.GuildMismatch;
        }
        var guildIdStr = guild.Id.ToString();
        var roleIdStr = role.Id.ToString();
        if (await db.RolePreserveGuilds.FindAsync(guildIdStr) == null)
        {
            await db.RolePreserveGuilds.AddAsync(new RolePreserveGuildModel
            {
                GuildId = guildIdStr,
                Enabled = false
            });
        }

        // already exists
        if (await db.RolePreserveBlacklistedRoles.FindAsync(guildIdStr, roleIdStr) != null)
        {
            return RoleBlacklistAddResult.AlreadyExists;
        }

        var userIdStr = doneByUser?.Id.ToString();
        await db.RolePreserveBlacklistedRoles.AddAsync(new RolePreserveBlacklistedRoleModel()
        {
            GuildId = guildIdStr,
            RoleId = roleIdStr,
            CreatedByUserId = userIdStr
        });
        await db.RolePreserveAudit.AddAsync(new RolePreserveAuditModel()
        {
            GuildId = guildIdStr,
            Action = RolePreserveAuditAction.BlacklistAdd,
            UserId = userIdStr,
            TargetRoleId = roleIdStr
        });
        return RoleBlacklistAddResult.Ok;
    }

    public enum RoleBlacklistAddResult
    {
        Ok,
        AlreadyExists,
        GuildMismatch
    }

    public Task<RoleBlacklistRemoveResult> RoleBlacklistRemove(
        XeniaDbContext db,
        IGuild guild,
        ulong roleId,
        IUser? doneByUser = null)
        => RoleBlacklistRemove(db, guild.Id, roleId, doneByUser);

    public async Task<RoleBlacklistRemoveResult> RoleBlacklistRemove(
        XeniaDbContext db,
        ulong guildId,
        ulong roleId,
        IUser? doneByUser = null)
    {
        var guildIdStr = guildId.ToString();
        var roleIdStr = roleId.ToString();
        var target = await db.RolePreserveBlacklistedRoles.FindAsync(guildIdStr, roleIdStr);
        if (target == null) return RoleBlacklistRemoveResult.NotFound;
        var userIdStr = doneByUser?.Id.ToString();
        db.Remove(target);
        await db.RolePreserveAudit.AddAsync(new RolePreserveAuditModel()
        {
            GuildId = guildIdStr,
            Action = RolePreserveAuditAction.BlacklistRemove,
            UserId = userIdStr,
            TargetRoleId = roleIdStr
        });
        return RoleBlacklistRemoveResult.Ok;
    }

    public async Task<RoleBlacklistRemoveResult> RoleBlacklistAddRange(
        XeniaDbContext db,
        ulong guildId,
        ulong[] roleIds,
        IUser? doneByUser = null,
        DateTime? now = null)
    {
        roleIds = [.. roleIds.Distinct().Where(e => e > 0)];
        
        if (roleIds.Length == 0) return RoleBlacklistRemoveResult.Ok;
        
        var guildIdStr = guildId.ToString();
        var nowValue = now.GetValueOrDefault(DateTime.UtcNow);
        var roleIdStrs = roleIds.Select(e => e.ToString()).ToArray();
        
        var existing = await db.RolePreserveBlacklistedRoles
            .Where(e => ((IEnumerable<string>)roleIdStrs).Contains(e.RoleId))
            .Select(e => e.RoleId)
            .ToArrayAsync();

        var addRange = new List<RolePreserveBlacklistedRoleModel>();
        var addedRoles = new HashSet<string>();
        foreach (var roleId in roleIds.Select(e => e.ToString()))
        {
            if (existing.Contains(roleId)) continue;
            if (addedRoles.Add(roleId))
            {
                addRange.Add(new RolePreserveBlacklistedRoleModel()
                {
                    GuildId = guildIdStr,
                    RoleId = roleId,
                    CreatedByUserId = doneByUser?.Id.ToString(),
                    CreatedAt = nowValue
                });
            }
        }

        await db.AddRangeAsync(addRange);

        await AuditAddRange(
            db,
            guildId,
            roleIds,
            RolePreserveAuditAction.BlacklistAdd,
            doneByUser?.Id,
            null,
            now);
        return RoleBlacklistRemoveResult.Ok;
    }
    public async Task<RoleBlacklistRemoveResult> RoleBlacklistRemoveRange(
        XeniaDbContext db,
        ulong guildId,
        ulong[] roleIds,
        IUser? doneByUser = null,
        DateTime? now = null)
    {
        roleIds = [.. roleIds.Distinct().Where(e => e > 0)];

        if (roleIds.Length == 0) return RoleBlacklistRemoveResult.Ok;
        
        var roleIdStrs = roleIds.Select(e => e.ToString()).ToArray();
        
        // "object[]" is done to keep "db.RemoveRange" happy
        object[] existing = await db.RolePreserveBlacklistedRoles
            .Where(e => ((IEnumerable<string>)roleIdStrs).Contains(e.RoleId))
            .ToArrayAsync();
        
        if (existing.Length == 0) return RoleBlacklistRemoveResult.NotFound;
        
        db.RemoveRange(existing);

        await AuditAddRange(
            db,
            guildId,
            roleIds,
            RolePreserveAuditAction.BlacklistRemove,
            doneByUser?.Id,
            null,
            now);

        return RoleBlacklistRemoveResult.Ok;
    }

    private async Task<RolePreserveAuditModel?> AuditAddRange(
        XeniaDbContext db,
        ulong guildId,
        ulong[] roleIds,
        RolePreserveAuditAction action,
        ulong? userId,
        ulong? targetUserId,
        DateTime? now = null,
        Dictionary<ulong, (RolePreserveAuditAppliedRoleAction AppliedAction, string? ExceptionText)>? appliedRolesDict = null)
    {
        roleIds = [.. roleIds.Distinct().Where(e => e > 0)];
        if ((action == RolePreserveAuditAction.AppliedRoles ||
             action == RolePreserveAuditAction.BlacklistAdd ||
             action == RolePreserveAuditAction.BlacklistRemove)
            && roleIds.Length == 0) return null;

        var nowValue = now.GetValueOrDefault(DateTime.UtcNow);
        var model = new RolePreserveAuditModel
        {
            GuildId = guildId.ToString(),
            Action = action,
            UserId = userId?.ToString(),
            TargetUserId = targetUserId?.ToString(),
            RecordCreatedAt = nowValue
        };
        if (action == RolePreserveAuditAction.AppliedRoles)
        {
            foreach (var roleId in roleIds)
            {
                var item = new RolePreserveAuditAppliedRoleModel
                {
                    RolePreserveAuditId = model.Id,
                    RoleId = roleId.ToString(),
                };
                if (appliedRolesDict?.TryGetValue(roleId, out var rs) == true)
                {
                    item.Action = rs.AppliedAction;
                    item.ExceptionText = rs.ExceptionText;
                }
                model.AppliedRoles.Add(item);
            }
        }
        else if (roleIds.Length == 1)
        {
            model.TargetRoleId = roleIds[0].ToString();
        }
        else if (roleIds.Length > 1)
        {
            model.ReferencedRoles.AddRange(roleIds
                .Distinct()
                .Select(e => new RolePreserveAuditReferencedRoleModel
                {
                    RolePreserveAuditId = model.Id,
                    RoleId = e.ToString(),
                }));
        }

        await db.AddAsync(model);
        return model;
    }

    public enum RoleBlacklistRemoveResult
    {
        Ok,
        NotFound
    }

    public Task EnableAsync(XeniaDbContext db, ulong guildId)
        => EnableAsync(db, guildId, true);
    public Task DisableAsync(XeniaDbContext db, ulong guildId)
        => EnableAsync(db, guildId, false);

    private static IQueryable<RolePreserveGuildModel> Apply(IQueryable<RolePreserveGuildModel> query, QueryOptions? options)
    {
        options ??= new QueryOptions();

        if (options.IncludeUsers)
        {
            query = query.Include(e => e.Users)
                .ThenInclude(e => e.Roles);
        }

        return query;
    }

    public class QueryOptions
    {
        public bool IncludeUsers { get; set; }
        public bool IncludeGuildMemberSnapshots { get; set; }
    }
}