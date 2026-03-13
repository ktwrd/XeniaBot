using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.Cache;

namespace XeniaDiscord.Common.Services;

public class UserCacheService
{
    private readonly XeniaDbContext _db;
    private readonly DiscordSocketClient _client;

    public UserCacheService(IServiceProvider services)
    {
        _db = services.GetRequiredScopedService<XeniaDbContext>(out var _);
        _client = services.GetRequiredService<DiscordSocketClient>();
    }
    public async Task<string?> GetDisplayAvatarUrl(ulong id)
    {
        var idStr = id.ToString();
        var dbRecord = await _db.UserCache
            .Where(e => e.Id == idStr)
            .Select(e => new { e.RecordUpdatedAt, e.DisplayAvatarUrl })
            .FirstOrDefaultAsync();
        if (dbRecord == null ||
            dbRecord.RecordUpdatedAt > (DateTime.UtcNow - TimeSpan.FromDays(365)))
        {
            var user = await _client.GetUserAsync(id);
            if (user == null) return dbRecord?.DisplayAvatarUrl;

            var url = user.GetDisplayAvatarUrl();

            await UpdateAsync(_db, user);
            await _db.SaveChangesAsync();

            return url;
        }
        return dbRecord.DisplayAvatarUrl;
    }

    public async Task UpdateAsync(XeniaDbContext db, IUser user)
    {
        var idStr = user.Id.ToString();
        await UpdateAsync(db, new UserCacheModel
        {
            Id = idStr,
            CreatedAt = user.CreatedAt.UtcDateTime,
            Username = user.Username,
            Discriminator = user.DiscriminatorValue == 0 ? null : user.Discriminator,
            GlobalName = string.IsNullOrEmpty(user.GlobalName?.Trim()) ? null : user.GlobalName,
            DisplayAvatarUrl = user.GetDisplayAvatarUrl(),
            RecordUpdatedAt = DateTime.UtcNow
        });
    }
    public async Task UpdateAsync(XeniaDbContext db, UserCacheModel model)
    {
        if (await db.UserCache.AnyAsync(e => e.Id == model.Id))
        {
            await db.UserCache.Where(e => e.Id == model.Id)
                .ExecuteUpdateAsync(e => e
                .SetProperty(p => p.Username, model.Username)
                .SetProperty(p => p.Discriminator, model.Discriminator)
                .SetProperty(p => p.GlobalName, model.GlobalName)
                .SetProperty(p => p.RecordUpdatedAt, DateTime.UtcNow)
                .SetProperty(p => p.DisplayAvatarUrl, model.DisplayAvatarUrl));
        }
        else
        {
            await db.UserCache.AddAsync(model);
        }
    }
}
