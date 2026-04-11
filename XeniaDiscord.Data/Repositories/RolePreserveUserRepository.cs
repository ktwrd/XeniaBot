using Microsoft.EntityFrameworkCore;
using NLog;
using XeniaDiscord.Data.Models.RolePreserve;
using XeniaDiscord.Data.Models.Snapshot;

namespace XeniaDiscord.Data.Repositories;

public class RolePreserveUserRepository
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    
    public async Task<RolePreserveUserModel?> GetAsync(
        XeniaDbContext db,
        ulong guildId,
        ulong userId,
        QueryOptions? options = null)
    {
        var guildIdStr = guildId.ToString();
        var userIdStr = userId.ToString();
        return await Apply(db.RolePreserveUsers, options)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.GuildId == guildIdStr && e.UserId == userIdStr);
    }

    public async Task InsertOrUpdate(
        XeniaDbContext db,
        ulong guildId,
        ulong userId,
        ulong[] roleIds,
        DateTime? start = null)
    {
        var guildIdStr = guildId.ToString();
        var userIdStr = userId.ToString();
        var now = start ?? DateTime.UtcNow;
        if (await db.RolePreserveGuilds.FindAsync(guildIdStr) == null)
        {
            await db.RolePreserveGuilds.AddAsync(new RolePreserveGuildModel()
            {
                GuildId = guildIdStr,
                Enabled = false,
                Users = null!
            });
            _log.Trace($"Created Record in {RolePreserveGuildModel.TableName} (GuildId={guildIdStr}, Enabled={false})");
        }
        if (await db.RolePreserveUsers.FindAsync(guildIdStr, userIdStr) != null)
        {
            await db.RolePreserveUsers.AsNoTracking()
                .Where(e => e.GuildId == guildIdStr && e.UserId == userIdStr)
                .ExecuteUpdateAsync(e => e.SetProperty(p => p.UpdatedAt, now));
        }
        else
        {
            var m = new RolePreserveUserModel
            {
                GuildId = guildIdStr,
                UserId = userIdStr,
                CreatedAt = now,
                UpdatedAt = now,
                Roles = null!,
                RolePreserveGuild = null!
            };
            if (await db.RolePreserveUsers.FindAsync(guildIdStr, userIdStr) == null)
            {
                await db.RolePreserveUsers.AddAsync(m);
            }
            _log.Trace($"Created Record in {RolePreserveUserModel.TableName} (GuildId={guildIdStr}, UserId={userIdStr})");
        }

        var roleIdStrArr = roleIds.Select(e => e.ToString()).ToArray();
        var currentRoleIds = await db.RolePreserveUserRoles
            .AsNoTracking()
            .Where(e => e.GuildId == guildIdStr && e.UserId == userIdStr)
            .Select(e => e.RoleId)
            .ToHashSetAsync();
        var delete = currentRoleIds.Where(e => !roleIdStrArr.Contains(e)).ToArray();
        var add = roleIdStrArr.Where(e => !currentRoleIds.Contains(e)).ToArray();

        await db.RolePreserveUserRoles
            .AsNoTracking()
            .Where(e => e.GuildId == guildIdStr && e.UserId == userIdStr && delete.Contains(e.RoleId))
            .ExecuteDeleteAsync();
        await db.RolePreserveUserRoles.AddRangeAsync(
            add.Select(roleIdStr => new RolePreserveUserRoleModel
            {
                GuildId = guildIdStr,
                UserId = userIdStr,
                RoleId = roleIdStr
            }));
        _log.Trace($"Updated {RolePreserveUserRoleModel.TableName} (deleted={delete.Length}, inserted={add.Length}, GuildId={guildIdStr}, UserId={userIdStr})");
    }

    public async Task InsertOrUpdate(
        XeniaDbContext db,
        RolePreserveUserModel model)
    {
        var roleIds = model.Roles.Select(e => e.GetRoleId()).Distinct().ToArray();
        await InsertOrUpdate(db, model.GetGuildId(), model.GetUserId(), roleIds, start: model.UpdatedAt);
    }

    public async Task UpdateSnapshot(
        XeniaDbContext db,
        GuildMemberSnapshotModel snapshot)
    {
        var roleIds = snapshot.Roles.Select(e => e.GetRoleId()).Distinct().ToArray();
        await InsertOrUpdate(db, snapshot.GetGuildId(), snapshot.GetUserId(), roleIds, start: snapshot.RecordCreatedAt);
    }

    public async Task<IReadOnlyCollection<ulong>> FindRolesForUser(
        XeniaDbContext db,
        ulong guildId,
        ulong userId)
    {
        var guildIdStr = guildId.ToString();
        var userIdStr = userId.ToString();
        var roleIds = await db.RolePreserveUserRoles.AsNoTracking()
            .Where(e => e.GuildId == guildIdStr && e.UserId == userIdStr)
            .Select(e => e.RoleId)
            .Distinct()
            .ToListAsync();
        return roleIds.Select(e => e.ParseULong(false).GetValueOrDefault(0))
            .Where(e => e > 0)
            .Distinct()
            .ToArray();
    }

    public async Task<bool> HasAny(
        XeniaDbContext db,
        ulong guildId,
        ulong userId)
    {
        var guildIdStr = guildId.ToString();
        var userIdStr = userId.ToString();
        return await db.RolePreserveUsers
            .AsNoTracking()
            .AnyAsync(e => e.GuildId == guildIdStr && e.UserId == userIdStr);
    }
    
    private static IQueryable<RolePreserveUserModel> Apply(
        IQueryable<RolePreserveUserModel> query,
        QueryOptions? options)
    {
        options ??= new QueryOptions();

        if (options.IncludeRolePreserveGuild)
        {
            query = query.Include(e => e.RolePreserveGuild);
        }
        if (options.IncludeRoles)
        {
            query = query.Include(e => e.Roles);
        }

        return query;
    }
    
    public class QueryOptions
    {
        public bool IncludeRolePreserveGuild { get; set; }
        public bool IncludeRoles { get; set; }
    }
}