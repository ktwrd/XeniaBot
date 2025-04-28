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
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var result = await collection.FindAsync(filter);
        var first = result.FirstOrDefault();
        return first;
    }

    public async Task Set(EconProfileModel model)
    {
        var filter = Builders<EconProfileModel>
            .Filter
            .Where(v => v.UserId == model.UserId && v.GuildId == model.GuildId);
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
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