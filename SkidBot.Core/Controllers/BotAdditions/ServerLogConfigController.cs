using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using SkidBot.Core.Models;
using SkidBot.Shared;

namespace SkidBot.Core.Controllers.BotAdditions;

[SkidController]
public class ServerLogConfigController : BaseConfigController<ServerLogModel>
{
    public ServerLogConfigController(IServiceProvider services)
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