using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using NLog;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Repositories;

[XeniaController]
public class RolePreserveRepository : BaseRepository<RolePreserveModel>
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public RolePreserveRepository(IServiceProvider services)
        : base(RolePreserveModel.CollectionName, services)
    {
        var collection = GetCollection();
        if (collection == null)
        {
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        }

        var collectionName = MongoCollectionName;

        var existingIndexes = collection.Indexes.List().ToList();
        var targetIndexes = new Dictionary<string, IndexKeysDefinition<RolePreserveModel>>()
        {
            {
                collectionName + "_IX_UserIdGuildId",
                Builders<RolePreserveModel>
                    .IndexKeys
                    .Descending(e => e.UserId)
                    .Descending(e => e.GuildId)
            }
        };
        foreach (var (name, idx) in targetIndexes)
        {
            if (!existingIndexes.Any(e => e.GetElement("name").Value.AsString == name))
            {
                var model = new CreateIndexModel<RolePreserveModel>(idx, new CreateIndexOptions()
                {
                    Name = name
                });
                try
                {
                    collection.Indexes.CreateOne(model);
                    _log.Info($"{collectionName} - Created index \"{name}\"");
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"{collectionName} - Failed to create index \"{name}\"");
                }
            }
        }
    }


    public async Task<RolePreserveModel?> Get(ulong userId, ulong guildId)
    {
        var filter = Builders<RolePreserveModel>
            .Filter
            .Where(v => v.UserId == userId && v.GuildId == guildId);
        var res = await BaseFind(filter, limit: 1);
        return await res.FirstOrDefaultAsync();
    }

    public async Task Set(RolePreserveModel model)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var filter = Builders<RolePreserveModel>
            .Filter
            .Where(v => v.UserId == model.UserId && v.GuildId == model.GuildId);

        var exists = await collection.CountDocumentsAsync(filter) > 0;

        if (exists)
        {
            await collection.DeleteManyAsync(filter);
        }
        await collection.InsertOneAsync(model);
    }
}