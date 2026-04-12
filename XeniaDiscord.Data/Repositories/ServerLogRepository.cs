using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using XeniaDiscord.Data.Models.ServerLog;

namespace XeniaDiscord.Data.Repositories;

// TODO Use IDbContextFactory<WeatherForecastContext> in methods that don't take XeniaDbContext as a parameter
// This should be done in *all* repositories
// https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics?tabs=with-di%2Cexpression-api-with-constant#dbcontext-pooling

public class ServerLogRepository
{
    private readonly XeniaDbContext _db;
    private readonly GuildCacheRepository _guildCacheRepo;
    private readonly DiscordSocketClient _discordClient;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public ServerLogRepository(IServiceProvider services)
    {
        _db = services.GetRequiredScopedService<XeniaDbContext>(out var scope);
        _guildCacheRepo = (scope?.ServiceProvider ?? services).GetRequiredService<GuildCacheRepository>();
        _discordClient = services.GetRequiredService<DiscordSocketClient>();
    }

    public async Task<bool> IsEnabled(ulong guildId)
    {
        await using var db = _db.CreateSession();
        return await IsEnabled(db, guildId);
    }
    public async Task<bool> IsEnabled(XeniaDbContext db, ulong guildId)
    {
        var guildIdStr = guildId.ToString();
        return await db.ServerLogGuilds.FindAsync(guildIdStr) != null
            && await db.ServerLogChannels.AnyAsync(e => e.GuildId == guildIdStr);
    }

    public async Task<ServerLogGuildModel?> GetGuild(ulong guildId, GuildQueryOptions? options = null)
    {
        await using var db = _db.CreateSession();
        return await GetGuild(db, guildId, options);
    }
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
        ulong guildId,
        ulong channelId,
        ChannelQueryOptions? options = null)
    {
        await using var db = _db.CreateSession();
        return await GetChannelsForGuild(db, guildId, channelId, options);
    }
    public async Task<IReadOnlyCollection<ServerLogChannelModel>> GetChannelsForGuild(
        XeniaDbContext db,
        ulong guildId,
        ulong channelId,
        ChannelQueryOptions? options = null)
    {
        var guildIdStr = guildId.ToString();
        var channelIdStr = channelId.ToString();
        var query = db.ServerLogChannels
            .Where(e => e.GuildId == guildIdStr && e.ChannelId == channelIdStr);
        return await Apply(query, options).ToListAsync();
    }
    public async Task<IReadOnlyCollection<ServerLogChannelModel>> GetChannelsForGuild(ulong guildId, ServerLogEvent[] events, ChannelQueryOptions? options = null)
    {
        await using var db = _db.CreateSession();
        return await GetChannelsForGuild(db, guildId, events, options);
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
    #region Set/Remove Channels

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
    #endregion

    #region Add/Remove Channel Events
    public async Task<ServerLogChannelModel?> AddChannelEvent(
        ulong guildId,
        ulong channelId,
        ServerLogEvent @event,
        IUser? byUserId = null)
    {
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            var result = await AddChannelEvent(db, guildId, channelId, @event, byUserId);
            await db.SaveChangesAsync();
            await trans.CommitAsync();
            return result;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    public async Task<ServerLogChannelModel?> AddChannelEvent(
        XeniaDbContext db,
        ulong guildId, ulong channelId,
        ServerLogEvent @event,
        IUser? byUserId = null)
    {
        var guildIdStr = guildId.ToString();
        var channelIdStr = channelId.ToString();
        if (await db.ServerLogChannels.AnyAsync(e => e.GuildId == guildIdStr && e.ChannelId == channelIdStr && e.Event == @event))
        {
            // already exists
            return null;
        }

        IGuild? guild = null;
        try
        {
            guild = _discordClient.GetGuild(guildId);
        }
        catch (Exception ex)
        {
            _log.Warn(ex, $"Failed to get Guild: {guildId}");
        }
        await _guildCacheRepo.Ensure(db, guildId, guild);

        if (!await db.ServerLogGuilds.AnyAsync(e => e.GuildId == guildIdStr))
        {
            await db.ServerLogGuilds.AddAsync(new ServerLogGuildModel()
            {
                GuildId = guildIdStr,
                Enabled = true
            });
        }

        var model = new ServerLogChannelModel
        {
            GuildId = guildIdStr,
            ChannelId = channelIdStr,
            Event = @event,
        };
        if (byUserId != null)
        {
            model.CreatedByUserId = byUserId.Id.ToString();
        }

        await db.ServerLogChannels.AddAsync(model);
        return model;
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
    #endregion

    #region Enable/Disable
    public async Task Enable(ulong guildId, bool enable = true)
    {
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            await Enable(db, guildId, enable);
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    public async Task Enable(XeniaDbContext db, ulong guildId, bool enable)
    {
        var guildIdStr = guildId.ToString();
        if (await db.ServerLogGuilds.FindAsync(guildIdStr) == null)
        {
            await db.AddAsync(new ServerLogGuildModel(guildId)
            {
                Enabled = enable
            });
        }
        else
        {
            await db.ServerLogGuilds.Where(e => e.GuildId == guildIdStr)
                .ExecuteUpdateAsync(e => e.SetProperty(p => p.Enabled, enable));
        }
    }
    public Task Disable(ulong guildId) => Enable(guildId, false);
    public Task Disable(XeniaDbContext db, ulong guildId) => Enable(db, guildId, false);
    #endregion

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
        if (await db.ServerLogGuilds.AnyAsync(e => e.GuildId == model.GuildId))
        {
            await db.ServerLogGuilds.Where(e => e.GuildId == model.GuildId)
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
        if (!await db.ServerLogGuilds.AnyAsync(e => e.GuildId == model.GuildId))
        {
            await db.ServerLogGuilds.AddAsync(new ServerLogGuildModel
            {
                GuildId = model.GuildId,
                Enabled = true
            });
        }
        if (await db.ServerLogChannels.AnyAsync(e => e.Id == model.Id))
        {
            if (model.UpdatedAt == model.CreatedAt)
            {
                model.UpdatedAt = DateTime.UtcNow;
            }
            await db.ServerLogChannels.Where(e => e.Id == model.Id)
                .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.Event, model.Event)
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
        if (options.IncludeServerLogGuild)
        {
            q = q.Include(e => e.ServerLogGuild);
            if (options.IncludeGuildCache)
            {
                q = q.Include(e => e.ServerLogGuild)
                    .ThenInclude(e => e.GuildCache);
            }
        }
        if (options.IgnoreDisabledGuilds)
        {
            q = q.Where(e => e.ServerLogGuild.Enabled);
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
        public bool IncludeServerLogGuild {get;set;} = false;
        public bool IgnoreDisabledGuilds { get; set; } = false;
    }
}