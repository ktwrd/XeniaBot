using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Repositories;

[XeniaController]
public class GuildGreeterConfigRepository : BaseRepository<GuildGreeterConfigModel>
{
    public GuildGreeterConfigRepository(IServiceProvider services)
        : base(GuildGreeterConfigModel.CollectionName, services)
    {}

    public async Task<GuildGreeterConfigModel?> GetLatest(ulong guildId)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var filter = Builders<GuildGreeterConfigModel>
            .Filter
            .Where(e => e.GuildId == guildId);
        var sort = Builders<GuildGreeterConfigModel>
            .Sort
            .Descending(e => e.ModifiedAtTimestamp);
        var res = await BaseFind(filter, sort, limit: 1);
        return res.FirstOrDefault();
    }

    public async Task Add(GuildGreeterConfigModel model)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        model.ModifiedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        model.ResetId();
        await collection.InsertOneAsync(model);
    }
}