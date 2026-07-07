using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.Cache;
using XeniaDiscord.Data.Repositories;

namespace XeniaDiscord.Common.Services;

public class UserCacheService
{
    private readonly IDbContextFactory<XeniaDbContext> _dbContextFactory;
    private readonly DiscordSocketClient _client;
    private readonly UserCacheRepository _repo;
    private readonly IMapper<IUser, UserCacheModel> _mapper;
    private readonly IMapperMerger<IUser, UserCacheModel> _mapperMerger;

    public UserCacheService(IServiceProvider services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        _dbContextFactory = services.GetRequiredService<IDbContextFactory<XeniaDbContext>>();
        _repo = services.GetRequiredService<UserCacheRepository>();
        _mapper = services.GetRequiredService<IMapper<IUser, UserCacheModel>>();
        _mapperMerger = services.GetRequiredService<IMapperMerger<IUser, UserCacheModel>>();
    }
    public async Task<string?> GetDisplayAvatarUrl(ulong id, bool saveChanges = true)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await GetDisplayAvatarUrl(db, id, saveChanges);
    }
    
    public async Task<string?> GetDisplayAvatarUrl(XeniaDbContext db, ulong id, bool saveChanges = true)
    {
        var idStr = id.ToString();
        var dbRecord = await db.UserCache
            .AsNoTracking()
            .Where(e => e.Id == idStr)
            .FirstOrDefaultAsync();
        if (dbRecord == null ||
            dbRecord.RecordUpdatedAt < (DateTime.UtcNow - TimeSpan.FromDays(365)))
        {
            var user = ExceptionHelper.RetryOnTimedOut(() => _client.GetUser(id));
            if (user == null) return dbRecord?.DisplayAvatarUrl;

            var mapped = dbRecord == null ? _mapper.Map(user) : _mapperMerger.Map(dbRecord, user);
            await _repo.InsertOrUpdate(_db, mapped);
            if (saveChanges)
            {
                await db.SaveChangesAsync();
            }

            return mapped.DisplayAvatarUrl;
        }
        return dbRecord.DisplayAvatarUrl;
    }
}
