using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Repositories;

[XeniaController]
public class LevelSystemConfigRepository : BaseRepository<LevelSystemConfigModel>
{
    public LevelSystemConfigRepository(IServiceProvider services)
        : base("levelSystem_GuildConfig", services)
    {}
    
    public async Task<LevelSystemConfigModel?> Get(ulong guildId)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var filter = Builders<LevelSystemConfigModel>
            .Filter
            .Where(e => e.GuildId == guildId);

        var result = await BaseFind(filter, limit: 1);
        var first = await result.FirstOrDefaultAsync();
        if (first == null)
            first = new LevelSystemConfigModel()
            {
                GuildId = guildId
            };
        if (first.RoleGrant == null)
            first.RoleGrant = new List<LevelSystemRoleGrantItem>();
        return first;
    }

    public async Task Set(LevelSystemConfigModel model)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var filter = Builders<LevelSystemConfigModel>
            .Filter
            .Where(e => e.GuildId == model.GuildId);

        var existResult = await BaseFind(filter, limit: 1);
        var exists = await existResult.AnyAsync();

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