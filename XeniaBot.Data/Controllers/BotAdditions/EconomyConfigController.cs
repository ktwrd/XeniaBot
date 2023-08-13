using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Controllers.BotAdditions;

[BotController]
public class EconomyConfigController : BaseConfigController<EconProfileModel>
{
    public EconomyConfigController(IServiceProvider services)
        : base("econData", services)
    {
    }

    public async Task<EconProfileModel?> Get(ulong userId, ulong guildId)
    {
        var filter = Builders<EconProfileModel>
            .Filter
            .Where(v => v.UserId == userId && v.GuildId == guildId);
        var result = await GetCollection().FindAsync(filter);
        var first = result.FirstOrDefault();
        return first;
    }

    public async Task Set(EconProfileModel model)
    {
        var filter = Builders<EconProfileModel>
            .Filter
            .Where(v => v.UserId == model.UserId && v.GuildId == model.GuildId);
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