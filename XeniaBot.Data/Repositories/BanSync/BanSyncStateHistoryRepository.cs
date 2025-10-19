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
    {
        var collection = GetCollection();
        if (collection == null)
        {
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        }

        var collectionName = MongoCollectionName;

        var existingIndexes = collection.Indexes.List().ToList();
        var targetIndexes = new Dictionary<string, IndexKeysDefinition<BanSyncStateHistoryItemModel>>()
        {
            {
                collectionName + "_IX_GuildId",
                Builders<BanSyncStateHistoryItemModel>
                    .IndexKeys
                    .Descending(e => e.GuildId)
            },
            {
                collectionName + "_IX_GuildIdTimestamp",
                Builders<BanSyncStateHistoryItemModel>
                    .IndexKeys
                    .Descending(e => e.GuildId)
                    .Descending(e => e.Timestamp)
            },
            {
                collectionName + "_IX_Timestamp",
                Builders<BanSyncStateHistoryItemModel>
                    .IndexKeys
                    .Descending(e => e.Timestamp)
            }
        };
        foreach (var (name, idx) in targetIndexes)
        {
            if (!existingIndexes.Any(e => e.GetElement("name").Value.AsString == name))
            {
                var model = new CreateIndexModel<BanSyncStateHistoryItemModel>(idx, new CreateIndexOptions()
                {
                    Name = name
                });
                try
                {
                    collection.Indexes.CreateOne(model);
                    Log.WriteLine($"{collectionName} - Created index \"{name}\"");
                }
                catch (Exception ex)
                {
                    Log.Error($"{collectionName} - Failed to create index \"{name}\"", ex);
                }
            }
        }
    }

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