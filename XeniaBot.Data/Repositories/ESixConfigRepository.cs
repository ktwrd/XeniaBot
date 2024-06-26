using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Repositories;

[XeniaController]
public class ESixConfigRepository : BaseRepository<ESixConfigModel>
{
    public ESixConfigRepository(IServiceProvider services)
        : base("esixGuildConfig", services)
    {
        
    }
    public async Task<ESixConfigModel?> Get(ulong guildId)
    {
        var filter = Builders<ESixConfigModel>
            .Filter
            .Eq("GuildId", guildId);
        var res = await GetCollection().FindAsync(filter);
        var single = res.FirstOrDefault();
        return single;
    }

    public async Task Set(ESixConfigModel model)
    {
        var filter = Builders<ESixConfigModel>
            .Filter
            .Eq("GuildId", model.GuildId);
        var collection = GetCollection();
        var result = await collection.FindAsync(filter);
        var exists = await result.AnyAsync();
        if (exists)
        {
            await collection.ReplaceOneAsync(filter, model);
        }
        else
        {
            await collection.InsertOneAsync(model);
        }
    }
}