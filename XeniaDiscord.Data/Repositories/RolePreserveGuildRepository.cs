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

    public async Task EnableAsync(XeniaDbContext db, ulong guildId, bool enable)
    {
        var guildIdStr = guildId.ToString();
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

        await db.RolePreserveBlacklistedRoles.AddAsync(new RolePreserveBlacklistedRoleModel()
        {
            GuildId = guildIdStr,
            RoleId = roleIdStr,
            CreatedByUserId = doneByUser?.Id.ToString()
        });
        return RoleBlacklistAddResult.Ok;
    }

    public enum RoleBlacklistAddResult
    {
        Ok,
        AlreadyExists,
        GuildMismatch
    }

    public async Task<RoleBlacklistRemoveResult> RoleBlacklistRemove(
        XeniaDbContext db,
        ulong guildId,
        ulong roleId)
    {
        var guildIdStr = guildId.ToString();
        var roleIdStr = roleId.ToString();
        var target = await db.RolePreserveBlacklistedRoles.FindAsync(guildIdStr, roleIdStr);
        if (target == null) return RoleBlacklistRemoveResult.NotFound;
        
        db.Remove(target);
        return RoleBlacklistRemoveResult.Ok;

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