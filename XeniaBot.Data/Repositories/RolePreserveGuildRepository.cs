using System;
using System.Data;
using System.Threading.Tasks;
using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Repositories;

[XeniaController]
public class RolePreserveGuildRepository : BaseRepository<RolePreserveGuildModel>
{
    public RolePreserveGuildRepository(IServiceProvider services)
        : base(RolePreserveGuildModel.CollectionName, services)
    {}

    
    public async Task<RolePreserveGuildModel?> Get(ulong guildId)
    {
        var filter = Builders<RolePreserveGuildModel>
            .Filter
            .Where(e => e.GuildId == guildId);
        var res = await BaseFind(filter, limit: 1);
        return res.FirstOrDefault();
    }
    public async Task Set(RolePreserveGuildModel model)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var filter = Builders<RolePreserveGuildModel>
            .Filter
            .Where(e => e.GuildId == model.GuildId);

        var exists = await collection.CountDocumentsAsync(filter) > 0;

        if (exists)
        {
            await collection.DeleteManyAsync(filter);
        }
        await collection.InsertOneAsync(model);
    }
}