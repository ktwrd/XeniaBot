using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Repositories;

[XeniaController]
public class RolePreserveRepository : BaseRepository<RolePreserveModel>
{
    public RolePreserveRepository(IServiceProvider services)
        : base(RolePreserveModel.CollectionName, services)
    { }


    public async Task<RolePreserveModel?> Get(ulong userId, ulong guildId)
    {
        var collection = GetCollection();
        var filter = Builders<RolePreserveModel>
            .Filter
            .Where(v => v.UserId == userId && v.GuildId == guildId);
        var res = await collection.FindAsync(filter);
        return res.FirstOrDefault();
    }

    public async Task Set(RolePreserveModel model)
    {
        var collection = GetCollection();
        var filter = Builders<RolePreserveModel>
            .Filter
            .Where(v => v.UserId == model.UserId && v.GuildId == model.GuildId);

        var existResult = await collection.FindAsync(filter);
        var exists = existResult.Any();

        if (exists)
        {
            await collection.DeleteManyAsync(filter);
        }
        await collection.InsertOneAsync(model);
    }
}