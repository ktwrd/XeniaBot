using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Repositories;

[XeniaController]
public class ServerLogRepository : BaseRepository<ServerLogModel>
{
    public ServerLogRepository(IServiceProvider services)
        : base("serverLogConfig", services)
    {
    }

    public async Task<ServerLogModel?> Get(ulong serverId)
    {
        var collection = GetCollection();
        var filter = Builders<ServerLogModel>
            .Filter
            .Eq("ServerId", serverId);

        var result = await collection.FindAsync(filter);
        var first = await result.FirstOrDefaultAsync();
        if (first == null)
            first = new ServerLogModel()
            {
                ServerId = serverId
            };
        return first;
    }

    public async Task Set(ServerLogModel model)
    {
        var collection = GetCollection();
        var filter = Builders<ServerLogModel>
            .Filter
            .Eq("ServerId", model.ServerId);

        var existResult = await collection.FindAsync(filter);
        var exists = existResult.Any();

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