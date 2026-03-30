using Microsoft.EntityFrameworkCore;
using NLog;
using XeniaDiscord.Data.Models.ServerLog;

namespace XeniaDiscord.Data.Repositories;

public class ServerLogRepository
{
    private readonly XeniaDbContext _db;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public ServerLogRepository(IServiceProvider services)
    {
        _db = services.GetRequiredScopedService<XeniaDbContext>(out var scope);
    }

    public Task<ServerLogGuildModel?> GetGuild(ulong guildId, GuildQueryOptions? options = null)
        => GetGuild(_db, guildId, options);
    public async Task<ServerLogGuildModel?> GetGuild(
        XeniaDbContext db,
        ulong guildId,
        GuildQueryOptions? options = null)
    {
        var guildIdStr = guildId.ToString();
        if (!await db.ServerLogGuilds.AnyAsync(e => e.GuildId == guildIdStr))
            return null;
        return await Apply(db.ServerLogGuilds.Where(e => e.GuildId == guildIdStr), options)
            .FirstOrDefaultAsync();
    }

    public async Task<ServerLogChannelModel?> GetChannel(
        XeniaDbContext db,
        ServerLogChannelModel model,
        ChannelQueryOptions? options = null)
    {
        return await Apply(db.ServerLogChannels.Where(e => e.Id == model.Id), options)
            .FirstOrDefaultAsync();
    }
    public async Task<IReadOnlyCollection<ServerLogChannelModel>> GetChannelsForGuild(
        XeniaDbContext db,
        ulong guildId,
        ChannelQueryOptions? options = null)
    {
        var guildIdStr = guildId.ToString();
        var query = db.ServerLogChannels
            .Where(e => e.GuildId == guildIdStr);

        return await Apply(query, options)
            .ToListAsync();
    }
    public async Task<IReadOnlyCollection<ServerLogChannelModel>> GetChannelsForGuild(
        XeniaDbContext db,
        ulong guildId,
        ServerLogEvent[] events,
        ChannelQueryOptions? options = null)
    {
        var guildIdStr = guildId.ToString();
        var eventsSet = events.ToHashSet();
        var query = db.ServerLogChannels
            .Where(e => e.GuildId == guildIdStr && eventsSet.Contains(e.Event));

        return await Apply(query, options)
            .ToListAsync();
    }

    public async Task SetChannels(
        ulong guildId,
        params IEnumerable<ServerLogChannelModel> channels)
    {
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            await SetChannels(db, guildId, channels);
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to set channels for Guild {guildId}");
            await trans.RollbackAsync();
            throw;
        }
    }

    public async Task SetChannels(
        XeniaDbContext db,
        ulong guildId,
        params IEnumerable<ServerLogChannelModel> channels)
    {
        var channelsArray = channels.ToArray();
        var guildIdStr = guildId.ToString();
        if (channelsArray.Count(e => e.GuildId == guildIdStr) != channelsArray.Length)
        {
            throw new ArgumentException("Not all channels are for the Guild Id provided!",
                nameof(channels));
        }

        if (channelsArray.Length < 1)
            throw new ArgumentException("Must have at least one item in array", nameof(channels));

        await db.ServerLogChannels
            .Where(e => e.GuildId == guildIdStr)
            .ExecuteDeleteAsync();
        await db.ServerLogChannels.AddRangeAsync(channelsArray);
        _log.Debug($"Set channels (GuildId={guildId},Count={channelsArray.Length})");
    }

    public async Task<int> RemoveChannel(
        ulong guildId, ulong channelId)
    {
        await using var trans = await _db.Database.BeginTransactionAsync();
        try
        {
            var result = await RemoveChannel(_db, guildId, channelId);
            await _db.SaveChangesAsync();
            await trans.CommitAsync();
            return result;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }
    public async Task<int> RemoveChannel(
        XeniaDbContext db,
        ulong guildId,
        ulong channelId)
    {
        var guildIdStr = guildId.ToString();
        var channelIdStr = channelId.ToString();
        if (!await db.ServerLogChannels.AnyAsync(e => e.GuildId == guildIdStr && e.ChannelId == channelIdStr))
        {
            return 0;
        }

        var result = await db.ServerLogChannels
            .Where(e => e.GuildId == guildIdStr && e.ChannelId == channelIdStr)
            .ExecuteDeleteAsync();
        _log.Debug($"Removed channels (GuildId={guildId},ChannelId={channelId},Count={result})");
        return result;
    }

    public async Task<int> RemoveEvents(ulong guildId, ServerLogEvent[] events)
    {
        await using var trans = await _db.Database.BeginTransactionAsync();
        try
        {
            var result = await RemoveEvents(_db, guildId, events);
            await _db.SaveChangesAsync();
            await trans.CommitAsync();
            return result;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }
    public async Task<int> RemoveEvents(
        XeniaDbContext db,
        ulong guildId,
        ServerLogEvent[] events)
    {
        if (events.Length < 1)
        {
            return 0;
        }
        
        var guildIdStr = guildId.ToString();
        var eventsSet = events.ToHashSet();
        var result = await db.ServerLogChannels
            .Where(e => e.GuildId == guildIdStr && eventsSet.Contains(e.Event))
            .ExecuteDeleteAsync();
        _log.Debug($"Removed events (GuildId={guildId},Events={string.Join(' ', events.Select(e => e.ToString()))},Count={result})");
        return result;
    }

    public async Task<bool> SetChannelEnabled(ulong guildId, ulong channelId, bool enabled)
    {
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            var result = await SetChannelEnabled(db, guildId, channelId, enabled);
            await db.SaveChangesAsync();
            await trans.RollbackAsync();
            return result;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }
    public async Task<bool> SetChannelEnabled(
        XeniaDbContext db,
        ulong guildId,
        ulong channelId,
        bool enabled)
    {
        var guildIdStr = guildId.ToString();
        var channelIdStr = channelId.ToString();
        return await db.ServerLogChannels.Where(e => e.GuildId == guildIdStr && e.ChannelId == channelIdStr)
            .ExecuteUpdateAsync(e => e
                .SetProperty(p => p.Enabled, enabled)) > 0;
    }


    #region Insert or Update
    public async Task InsertOrUpdate(ServerLogGuildModel model)
    {
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            await InsertOrUpdate(db, model);
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }
    public async Task InsertOrUpdate(
        XeniaDbContext db,
        ServerLogGuildModel model)
    {
        var guildIdStr = model.GuildId.ToString();
        if (await db.ServerLogGuilds.AnyAsync(e => e.GuildId == guildIdStr))
        {
            await db.ServerLogGuilds.Where(e => e.GuildId == guildIdStr)
                .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.Enabled, model.Enabled));
            _log.Debug($"Updated record (GuildId={model.GuildId})");
        }
        else
        {
            await db.ServerLogGuilds.AddAsync(model);
            _log.Debug($"Created record (GuildId={model.GuildId})");
        }
    }

    public async Task InsertOrUpdate(ServerLogChannelModel model)
    {
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            await InsertOrUpdate(db, model);
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }
    public async Task InsertOrUpdate(
        XeniaDbContext db,
        ServerLogChannelModel model)
    {
        if (await db.ServerLogChannels.AnyAsync(e => e.Id == model.Id))
        {
            if (model.UpdatedAt == model.CreatedAt)
            {
                model.UpdatedAt = DateTime.UtcNow;
            }
            await db.ServerLogChannels.Where(e => e.Id == model.Id)
                .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.Event, model.Event)
                    .SetProperty(p => p.Enabled, model.Enabled)
                    .SetProperty(p => p.UpdatedAt, model.UpdatedAt)
                    .SetProperty(p => p.UpdatedByUserId, model.UpdatedByUserId));
            _log.Debug($"Updated record (GuildId={model.GuildId},ChannelId={model.ChannelId},Event={model.Event})");
        }
        else
        {
            await db.ServerLogChannels.AddAsync(model);
            _log.Debug($"Created record (GuildId={model.GuildId},ChannelId={model.ChannelId},Event={model.Event})");
        }
    }
    #endregion

    private static IQueryable<ServerLogGuildModel> Apply(
        IQueryable<ServerLogGuildModel> query,
        GuildQueryOptions? options = null)
    {
        options ??= new();
        var q = query;
        if (options.IncludeChannels)
        {
            if (options.IncludeGuildCache)
            {
                q = q.Include(e => e.ServerLogChannels)
                    .ThenInclude(e => e.GuildCache);
            }
            else
            {
                q = q.Include(e => e.ServerLogChannels);
            }
        }

        if (options.IncludeGuildCache)
        {
            q = q.Include(e => e.GuildCache);
        }

        if (options.IgnoreDisabled)
        {
            q = q.Where(e => e.Enabled);
        }

        return q.AsNoTracking();
    }
    private static IQueryable<ServerLogChannelModel> Apply(
        IQueryable<ServerLogChannelModel> query,
        ChannelQueryOptions? options = null)
    {
        options ??= new();
        var q = query;
        if (options.IncludeGuildCache)
        {
            q = q.Include(e => e.GuildCache);
        }
        return q.AsNoTracking();
    }

    public class GuildQueryOptions
    {
        public bool IncludeChannels { get; set; } = false;
        public bool IncludeGuildCache { get; set; } = false;
        public bool IgnoreDisabled { get; set; } = false;
    }
    public class ChannelQueryOptions
    {
        public bool IncludeGuildCache { get; set; } = false;
    }
}