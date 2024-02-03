using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Controllers.BotAdditions;

[XeniaController]
public class LevelSystemGuildConfigController : BaseConfigController<LevelSystemGuildConfigModel>
{
    public LevelSystemGuildConfigController(IServiceProvider services)
        : base("levelSystem_GuildConfig", services)
    {}
    
    public async Task<LevelSystemGuildConfigModel?> Get(ulong guildId)
    {
        var collection = GetCollection();
        var filter = Builders<LevelSystemGuildConfigModel>
            .Filter
            .Eq("GuildId", guildId);

        var result = await collection.FindAsync(filter);
        var first = await result.FirstOrDefaultAsync();
        if (first == null)
            first = new LevelSystemGuildConfigModel()
            {
                GuildId = guildId
            };
        if (first.RoleGrant == null)
            first.RoleGrant = new List<LevelSystemRoleGrantItem>();
        return first;
    }

    public async Task Set(LevelSystemGuildConfigModel model)
    {
        var collection = GetCollection();
        var filter = Builders<LevelSystemGuildConfigModel>
            .Filter
            .Eq("GuildId", model.GuildId);

        var existResult = await collection.FindAsync(filter);
        var exists = existResult.Any();

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