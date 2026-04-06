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
        Guid snapshotId)
    {
        var guildIdStr = guildId.ToString();
        var userIdStr = userId.ToString();
        if (!await db.RolePreserveGuilds.AnyAsync(e => e.GuildId == guildIdStr))
        {
            await db.RolePreserveGuilds.AddAsync(new RolePreserveGuildModel()
            {
                GuildId = guildIdStr,
                Enabled = false
            });
            _log.Trace($"Created Record in {RolePreserveGuildModel.TableName} (GuildId={guildIdStr}, Enabled={false})");
        }
        if (await db.RolePreserveUsers.AnyAsync(e => e.GuildId == guildIdStr && e.UserId == userIdStr))
        {
            await db.RolePreserveUsers
                .Where(e => e.GuildId == guildIdStr && e.UserId == userIdStr)
                .ExecuteUpdateAsync(e => e
                                        .SetProperty(p => p.GuildMemberSnapshotId, snapshotId)
                                        .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));
            _log.Trace($"Updated Record in {RolePreserveUserModel.TableName} (GuildId={guildIdStr}, UserId={userIdStr}, SnapshotId={snapshotId})");
        }
        else
        {
            await db.RolePreserveUsers.AddAsync(new RolePreserveUserModel
            {
                GuildId = guildIdStr,
                UserId = userIdStr,
                GuildMemberSnapshotId = snapshotId
            });
            _log.Trace($"Created Record in {RolePreserveUserModel.TableName} (GuildId={guildIdStr}, UserId={userIdStr}, SnapshotId={snapshotId})");
        }
    }
    
    public async Task UpdateSnapshot(
        XeniaDbContext db,
        GuildMemberSnapshotModel snapshot)
    {
        if (!await db.RolePreserveGuilds.AnyAsync(e => e.GuildId == snapshot.GuildId))
        {
            await db.RolePreserveGuilds.AddAsync(new RolePreserveGuildModel
            {
                GuildId = snapshot.GuildId,
                Enabled = false
            });
            _log.Trace($"Created Record in {RolePreserveGuildModel.TableName} (GuildId={snapshot.GuildId}, Enabled={false})");
        }
        if (await db.RolePreserveUsers.AnyAsync(e => e.GuildId == snapshot.GuildId && e.UserId == snapshot.UserId))
        {
            await db.RolePreserveUsers
                .Where(e => e.GuildId == snapshot.GuildId && e.UserId == snapshot.UserId)
                .ExecuteUpdateAsync(e => e
                .SetProperty(p => p.GuildMemberSnapshotId, snapshot.RecordId)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));
            _log.Trace($"Updated Record in {RolePreserveUserModel.TableName} (GuildId={snapshot.GuildId}, UserId={snapshot.UserId}, SnapshotId={snapshot.RecordId})");
        }
        else
        {
            await db.RolePreserveUsers.AddAsync(new RolePreserveUserModel
            {
                GuildId = snapshot.GuildId,
                UserId = snapshot.UserId,
                GuildMemberSnapshotId = snapshot.RecordId
            });
            _log.Trace($"Created Record in {RolePreserveUserModel.TableName} (GuildId={snapshot.GuildId}, UserId={snapshot.UserId}, SnapshotId={snapshot.RecordId})");
        }
    }

    public async Task InsertOrUpdate(
        XeniaDbContext db,
        RolePreserveUserModel model)
    {
        if (!await db.RolePreserveGuilds.AnyAsync(e => e.GuildId == model.GuildId))
        {
            await db.RolePreserveGuilds.AddAsync(new RolePreserveGuildModel
            {
                GuildId = model.GuildId,
                Enabled = false
            });
            _log.Trace($"Created Record in {RolePreserveGuildModel.TableName} (GuildId={model.GuildId}, Enabled={false})");
        }
        if (await db.RolePreserveUsers.AnyAsync(e => e.GuildId == model.GuildId && e.UserId == model.UserId))
        {
            if (model.CreatedAt <= model.UpdatedAt)
            {
                model.UpdatedAt = DateTime.UtcNow;
            }

            await db.RolePreserveUsers
                .Where(e => e.GuildId == model.GuildId && e.UserId == model.UserId)
                .ExecuteUpdateAsync(e => e
                .SetProperty(p => p.GuildMemberSnapshotId, model.GuildMemberSnapshotId)
                .SetProperty(p => p.UpdatedAt, model.UpdatedAt));
            _log.Trace($"Updated Record in {RolePreserveUserModel.TableName} (GuildId={model.GuildId}, UserId={model.UserId}, SnapshotId={model.GuildMemberSnapshotId})");
        }
        else
        {
            model.UpdatedAt = model.CreatedAt;
            await db.RolePreserveUsers.AddAsync(model);
            _log.Trace($"Updated Record in {RolePreserveUserModel.TableName} (GuildId={model.GuildId}, UserId={model.UserId}, SnapshotId={model.GuildMemberSnapshotId})");
        }
    }

    public async Task<IReadOnlyCollection<GuildMemberRoleSnapshotModel>> FindRolesForUser(
        XeniaDbContext db,
        ulong guildId,
        ulong userId)
    {
        var guildIdStr = guildId.ToString();
        var userIdStr = userId.ToString();
        var roleIds = await db.RolePreserveUsers
            .Where(e => e.GuildId == guildIdStr && e.UserId == userIdStr)
            .Select(e => e.GuildMemberSnapshot)
            .SelectMany(e => e.Roles)
            .Include(e => e.GuildRoleSnapshot)
            .AsNoTracking()
            .ToListAsync();
        return roleIds.DistinctBy(e => e.RoleId).ToList();
    }

    public async Task<bool> HasAny(
        XeniaDbContext db,
        ulong guildId,
        ulong userId)
    {
        var guildIdStr = guildId.ToString();
        var userIdStr = userId.ToString();
        return await db.RolePreserveUsers
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
        if (options.IncludeGuildMemberSnapshots)
        {
            query = query.Include(e => e.GuildMemberSnapshot);
        }

        return query;
    }
    
    public class QueryOptions
    {
        public bool IncludeRolePreserveGuild { get; set; }
        public bool IncludeGuildMemberSnapshots { get; set; }
    }
}