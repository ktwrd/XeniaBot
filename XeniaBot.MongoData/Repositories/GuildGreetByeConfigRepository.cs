using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Repositories;

[XeniaController]
public class GuildGreetByeConfigRepository : BaseRepository<GuildByeGreeterConfigModel>
{
    public GuildGreetByeConfigRepository(IServiceProvider services)
        : base(GuildByeGreeterConfigModel.CollectionName, services)
    {}

    public async Task<GuildByeGreeterConfigModel?> GetLatest(ulong guildId)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var filter = Builders<GuildByeGreeterConfigModel>
            .Filter
            .Where(e => e.GuildId == guildId);
        var sort = Builders<GuildByeGreeterConfigModel>
            .Sort
            .Descending(e => e.ModifiedAtTimestamp);
        var res = await BaseFind(filter, sort, limit: 1);
        return res.FirstOrDefault();
    }

    public async Task Add(GuildByeGreeterConfigModel model)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        model.ModifiedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        model.ResetId();
        await collection.InsertOneAsync(model);
    }
}