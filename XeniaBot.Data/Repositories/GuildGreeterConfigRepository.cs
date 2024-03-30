using System;
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
        var filter = Builders<GuildGreeterConfigModel>
            .Filter
            .Eq("GuildId", guildId);
        var res = await collection.FindAsync(filter);
        var sorted = res.ToList().OrderByDescending(v => v.ModifiedAtTimestamp);
        return sorted.FirstOrDefault();
    }

    public async Task Add(GuildGreeterConfigModel model)
    {
        model.ResetId();
        model.ModifiedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var collection = GetCollection();
        await collection.InsertOneAsync(model);
    }
}