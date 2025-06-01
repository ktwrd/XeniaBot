using System;
using System.Collections.Generic;
using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;
using System.Linq;
using System.Threading.Tasks;
using System.Data;

namespace XeniaBot.Data.Repositories;

[XeniaController]
public class BanSyncStateHistoryRepository : BaseRepository<BanSyncStateHistoryItemModel>
{
    public BanSyncStateHistoryRepository(IServiceProvider services)
        : base(BanSyncStateHistoryItemModel.CollectionName, services)
    {}

    public async Task Add(BanSyncStateHistoryItemModel model)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        model.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        model.ResetId();
        await collection.InsertOneAsync(model);
    }

    public async Task<BanSyncStateHistoryItemModel?> GetOne(ulong guildId, long timestamp)
    {
        var filter = Builders<BanSyncStateHistoryItemModel>
            .Filter
            .Where(v => v.GuildId == guildId && v.Timestamp == timestamp);
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var result = await BaseFind(filter, limit: 1);
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
        var sort = Builders<BanSyncStateHistoryItemModel>
            .Sort
            .Descending(e => e.Timestamp);
        var result = await BaseFind(filter, sort, limit: 1);
        return result?.FirstOrDefault();
    }

    public async Task<BanSyncStateHistoryItemModel?> GetOldest(ulong guildId)
    {
        var filter = Builders<BanSyncStateHistoryItemModel>
            .Filter
            .Where(v => v.GuildId == guildId);
        var sort = Builders<BanSyncStateHistoryItemModel>
            .Sort
            .Ascending(e => e.Timestamp);
        var result = await BaseFind(filter, sort, limit: 1);
        return result?.FirstOrDefault();
    }
    
    public async Task<ICollection<BanSyncStateHistoryItemModel>?> GetMany(FilterDefinition<BanSyncStateHistoryItemModel> filter)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var result = await collection.FindAsync(filter);
        return await result.ToListAsync();
    }

    public Task<IAsyncCursor<BanSyncStateHistoryItemModel>> GetMany(ulong guildId)
    {
        var filter = Builders<BanSyncStateHistoryItemModel>
            .Filter
            .Where(v => v.GuildId == guildId);
        return BaseFind(filter);
    }
}