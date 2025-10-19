using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;
using XeniaBot.Shared;
using XeniaBot.Data.Models;
using System.Data;
using System.Collections.Generic;

namespace XeniaBot.Data.Repositories;

[XeniaController]
public class BanSyncConfigRepository : BaseRepository<ConfigBanSyncModel>
{
    private readonly BanSyncStateHistoryRepository _stateHistory;
    public BanSyncConfigRepository(IServiceProvider services)
        : base("banSyncGuildConfig", services)
    {
        _stateHistory = services.GetRequiredService<BanSyncStateHistoryRepository>();

        var collection = GetCollection();
        if (collection == null)
        {
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        }

        var collectionName = MongoCollectionName;

        var existingIndexes = collection.Indexes.List().ToList();
        var targetIndexes = new Dictionary<string, IndexKeysDefinition<ConfigBanSyncModel>>()
        {
            {
                collectionName + "_IX_GuildId",
                Builders<ConfigBanSyncModel>
                    .IndexKeys
                    .Descending(e => e.GuildId)
            }
        };
        foreach (var (name, idx) in targetIndexes)
        {
            if (!existingIndexes.Any(e => e.GetElement("name").Value.AsString == name))
            {
                var model = new CreateIndexModel<ConfigBanSyncModel>(idx, new CreateIndexOptions()
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

    public async Task<bool> Exists(ulong guildId)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var result = await collection.CountDocumentsAsync(GetFilter(guildId));
        return result > 0;
    }
    public async Task<ConfigBanSyncModel?> Get(ulong guildId)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var result = await BaseFind(GetFilter(guildId), limit: 1);

        return result.FirstOrDefault();
    }
    public async Task Set(ConfigBanSyncModel data)
    {
        var guildId = data.GuildId;
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var filter = GetFilter(guildId);
        if (await Exists(guildId))
        {
            await collection.ReplaceOneAsync(filter, data);
        }
        else
        {
            await collection.InsertOneAsync(data);
        }
        
        
        var stateHistoryItem = new BanSyncStateHistoryItemModel()
        {
            GuildId = data.GuildId,
            Enable = data.Enable,
            State = data.State,
            Reason = data.Reason
        };
        await _stateHistory.Add(stateHistoryItem);
    }
    protected FilterDefinition<ConfigBanSyncModel> GetFilter(ulong guildId)
    {
        var filter = Builders<ConfigBanSyncModel>
            .Filter
            .Where(e => e.GuildId == guildId);
        return filter;
    }
}
