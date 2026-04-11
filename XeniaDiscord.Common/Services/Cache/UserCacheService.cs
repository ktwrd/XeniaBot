using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.Cache;
using XeniaDiscord.Data.Repositories;

namespace XeniaDiscord.Common.Services;

public class UserCacheService
{
    private readonly XeniaDbContext _db;
    private readonly DiscordSocketClient _client;
    private readonly UserCacheRepository _repo;
    private readonly IMapper<IUser, UserCacheModel> _mapper;
    private readonly IMapperMerger<IUser, UserCacheModel> _mapperMerger;

    public UserCacheService(IServiceProvider services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        _db = services.GetRequiredScopedService<XeniaDbContext>(out var scope);
        _repo = (scope?.ServiceProvider ?? services).GetRequiredService<UserCacheRepository>();
        _mapper = services.GetRequiredService<IMapper<IUser, UserCacheModel>>();
        _mapperMerger = services.GetRequiredService<IMapperMerger<IUser, UserCacheModel>>();
    }
    public async Task<string?> GetDisplayAvatarUrl(ulong id, bool saveChages = true)
    {
        var idStr = id.ToString();
        var dbRecord = await _db.UserCache
            .AsNoTracking()
            .Where(e => e.Id == idStr)
            .FirstOrDefaultAsync();
        if (dbRecord == null ||
            dbRecord.RecordUpdatedAt < (DateTime.UtcNow - TimeSpan.FromDays(365)))
        {
            var user = await _client.GetUserAsync(id);
            if (user == null) return dbRecord?.DisplayAvatarUrl;

            var mapped = dbRecord == null ? _mapper.Map(user) : _mapperMerger.Map(dbRecord, user);
            await _repo.InsertOrUpdate(_db, mapped);
            if (saveChages)
            {
                await _db.SaveChangesAsync();
            }

            return mapped.DisplayAvatarUrl;
        }
        return dbRecord.DisplayAvatarUrl;
    }
}
