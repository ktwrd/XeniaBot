using System;
using System.Data;
using System.Threading.Tasks;
using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Repositories;

[XeniaController]
public class EconomyProfileRepository : BaseRepository<EconProfileModel>
{
    public EconomyProfileRepository(IServiceProvider services)
        : base("econData", services)
    {
    }

    public async Task<EconProfileModel?> Get(ulong userId, ulong guildId)
    {
        var filter = Builders<EconProfileModel>
            .Filter
            .Where(v => v.UserId == userId && v.GuildId == guildId);
        var result = await BaseFind(filter, limit: 1);
        return result.FirstOrDefault();
    }

    public async Task Set(EconProfileModel model)
    {
        var filter = Builders<EconProfileModel>
            .Filter
            .Where(v => v.UserId == model.UserId && v.GuildId == model.GuildId);
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