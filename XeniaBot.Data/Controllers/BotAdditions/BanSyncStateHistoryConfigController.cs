using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;
using System.Linq;

namespace XeniaBot.Data.Controllers.BotAdditions;

[XeniaController]
public class BanSyncStateHistoryConfigController : BaseConfigController<BanSyncStateHistoryItemModel>
{
    public BanSyncStateHistoryConfigController(IServiceProvider services)
        : base(BanSyncStateHistoryItemModel.CollectionName, services)
    {}

    public async Task Add(BanSyncStateHistoryItemModel model)
    {
        model._id = default;
        model.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var collection = GetCollection();
        await collection.InsertOneAsync(model);
    }

    public async Task<BanSyncStateHistoryItemModel?> GetOne(ulong guildId, long timestamp)
    {
        var filter = Builders<BanSyncStateHistoryItemModel>
            .Filter
            .Where(v => v.GuildId == guildId && v.Timestamp == timestamp);
        var collection = GetCollection();
        var result = await collection.FindAsync(filter);
        return result?.FirstOrDefault();
    }

    public Task<ICollection<BanSyncStateHistoryItemModel>?> GetAfter(ulong guildId, long timestamp)
    {
        var filter = Builders<BanSyncStateHistoryItemModel>
            .Filter
            .Where(v => v.GuildId == guildId && v.Timestamp > timestamp);
        return GetMany(filter);
    }

    public Task<ICollection<BanSyncStateHistoryItemModel>?> GetBefore(ulong guildId, long timestamp)
    {
        var filter = Builders<BanSyncStateHistoryItemModel>
            .Filter
            .Where(v => v.GuildId == guildId && v.Timestamp < timestamp);
        return GetMany(filter);
    }

    public async Task<BanSyncStateHistoryItemModel?> GetLatest(ulong guildId)
    {
        var filter = Builders<BanSyncStateHistoryItemModel>
            .Filter
            .Where(v => v.GuildId == guildId);
        var result = await GetMany(filter);
        var ordered = result?.OrderByDescending(v => v.Timestamp);
        return ordered?.FirstOrDefault();
    }

    public async Task<BanSyncStateHistoryItemModel?> GetOldest(ulong guildId)
    {
        var filter = Builders<BanSyncStateHistoryItemModel>
            .Filter
            .Where(v => v.GuildId == guildId);
        var result = await GetMany(filter);
        var ordered = result?.OrderBy(v => v.Timestamp);
        return ordered?.FirstOrDefault();
    }
    
    public async Task<ICollection<BanSyncStateHistoryItemModel>?> GetMany(FilterDefinition<BanSyncStateHistoryItemModel> filter)
    {
        var collection = GetCollection();
        var result = await collection.FindAsync(filter);
        return await result.ToListAsync();
    }

    public Task<ICollection<BanSyncStateHistoryItemModel>?> GetMany(ulong guildId)
    {
        var filter = Builders<BanSyncStateHistoryItemModel>
            .Filter
            .Where(v => v.GuildId == guildId);
        return GetMany(filter);
    }
}