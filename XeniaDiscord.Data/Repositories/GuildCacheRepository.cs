using Discord;
using NLog;
using XeniaDiscord.Data.Models.Cache;

namespace XeniaDiscord.Data.Repositories;

public class GuildCacheRepository
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public async Task Ensure(XeniaDbContext db, ulong guildId, IGuild? guild)
    {
        var guildIdStr = guildId.ToString();
        if (await db.GuildCache.FindAsync(guildIdStr) != null) return;
        var model = new GuildCacheModel()
        {
            Id = guildIdStr,
            Name = guild?.Name,
            CreatedAt = SnowflakeUtils.FromSnowflake(guildId).UtcDateTime,
            OwnerUserId = guild?.OwnerId.ToString(),
            IconUrl = guild?.IconUrl,
            BannerUrl = guild?.BannerUrl,
            SplashUrl = guild?.SplashUrl,
            DiscoverySplashUrl = guild?.DiscoverySplashUrl,
        };
        await db.GuildCache.AddAsync(model);
    }
    
    
    public async Task InsertOrUpdate(
        XeniaDbContext db,
        GuildCacheModel model)
    {
        model.RecordUpdatedAt = DateTime.UtcNow;
        var existing = await db.GuildCache.FindAsync(model.Id);
        if (existing != null)
        {
            existing.Name = model.Name;
            existing.OwnerUserId = model.OwnerUserId;
            existing.CreatedAt = model.CreatedAt;
            existing.JoinedAt = model.JoinedAt;
            existing.IconUrl = model.IconUrl;
            existing.BannerUrl = model.BannerUrl;
            existing.SplashUrl = model.SplashUrl;
            existing.DiscoverySplashUrl = model.DiscoverySplashUrl;
            existing.RecordUpdatedAt = model.RecordUpdatedAt;
            _log.Debug($"Updated record (Id={model.Id}, Name={model.Name})");
        }
        else
        {
            await db.GuildCache.AddAsync(model);
            _log.Debug($"Created record (Id={model.Id}, Name={model.Name})");
        }
    }
}
