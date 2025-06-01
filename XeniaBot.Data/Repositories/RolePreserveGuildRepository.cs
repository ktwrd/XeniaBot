using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Repositories;

[XeniaController]
public class RolePreserveGuildRepository : BaseRepository<RolePreserveGuildModel>
{
    public RolePreserveGuildRepository(IServiceProvider services)
        : base(RolePreserveGuildModel.CollectionName, services)
    {
        var collection = GetCollection();
        if (collection == null)
        {
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        }

        var collectionName = MongoCollectionName;

        var existingIndexes = collection.Indexes.List().ToList();
        var targetIndexes = new Dictionary<string, IndexKeysDefinition<RolePreserveGuildModel>>()
        {
            {
                collectionName + "_IX_GuildId",
                Builders<RolePreserveGuildModel>
                    .IndexKeys
                    .Descending(e => e.GuildId)
            }
        };
        foreach (var (name, idx) in targetIndexes)
        {
            if (!existingIndexes.Any(e => e.GetElement("name").Value.AsString == name))
            {
                var model = new CreateIndexModel<RolePreserveGuildModel>(idx, new CreateIndexOptions()
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

    
    public async Task<RolePreserveGuildModel?> Get(ulong guildId)
    {
        var filter = Builders<RolePreserveGuildModel>
            .Filter
            .Where(e => e.GuildId == guildId);
        var res = await BaseFind(filter, limit: 1);
        return res.FirstOrDefault();
    }
    public async Task Set(RolePreserveGuildModel model)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var filter = Builders<RolePreserveGuildModel>
            .Filter
            .Where(e => e.GuildId == model.GuildId);

        var exists = await collection.CountDocumentsAsync(filter) > 0;

        if (exists)
        {
            await collection.DeleteManyAsync(filter);
        }
        await collection.InsertOneAsync(model);
    }
}