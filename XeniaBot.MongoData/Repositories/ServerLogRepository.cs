using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using MongoDB.Driver;
using XeniaBot.MongoData.Models;
using XeniaBot.Shared;

namespace XeniaBot.MongoData.Repositories;

[XeniaController]
[Obsolete("Use XeniaDiscord.Data.Repositories.ServerLogRepository")]
public class ServerLogRepository : BaseRepository<ServerLogModel>
{
    public ServerLogRepository(IServiceProvider services)
        : base("serverLogConfig", services)
    {
    }

    public async Task<ServerLogModel?> Get(ulong serverId)
    {
        var filter = Builders<ServerLogModel>
            .Filter
            .Where(e => e.ServerId == serverId);

        var result = await BaseFind(filter, limit: 1);
        var first = await result.FirstOrDefaultAsync();
        if (first == null)
            first = new ServerLogModel()
            {
                ServerId = serverId
            };
        return first;
    }
    public async Task<List<ServerLogModel>> GetAll()
    {
        var result = await BaseFind(Builders<ServerLogModel>.Filter.Empty);
        return await result.ToListAsync();
    }

    public async Task Set(ServerLogModel model)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var filter = Builders<ServerLogModel>
            .Filter
            .Where(e => e.ServerId == model.ServerId);

        var exists = await collection.CountDocumentsAsync(filter) > 0;

        if (exists)
        {
            await collection.FindOneAndReplaceAsync(filter, model);
        }
        else
        {
            await collection.InsertOneAsync(model);
        }
    }
}