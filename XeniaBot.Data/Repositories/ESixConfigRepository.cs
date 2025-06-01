using System;
using System.Data;
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
            .Where(e => e.GuildId == guildId);
        var res = await BaseFind(filter, limit: 1);
        return res.FirstOrDefault();
    }

    public async Task Set(ESixConfigModel model)
    {
        var filter = Builders<ESixConfigModel>
            .Filter
            .Where(e => e.GuildId == model.GuildId);
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var result = await collection.CountDocumentsAsync(filter);
        if (result > 0)
        {
            await collection.ReplaceOneAsync(filter, model);
        }
        else
        {
            await collection.InsertOneAsync(model);
        }
    }
}