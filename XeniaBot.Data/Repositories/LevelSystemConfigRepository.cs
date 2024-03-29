using System;
using System.Collections.Generic;
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
        var filter = Builders<LevelSystemConfigModel>
            .Filter
            .Eq("GuildId", guildId);

        var result = await collection.FindAsync(filter);
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
        var filter = Builders<LevelSystemConfigModel>
            .Filter
            .Eq("GuildId", model.GuildId);

        var existResult = await collection.FindAsync(filter);
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