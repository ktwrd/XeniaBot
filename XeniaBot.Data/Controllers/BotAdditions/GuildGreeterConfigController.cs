using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Controllers.BotAdditions;

[BotController]
public class GuildGreeterConfigController : BaseConfigController<GuildGreeterConfigModel>
{
    public GuildGreeterConfigController(IServiceProvider services)
        : base(GuildGreeterConfigModel.CollectionName, services)
    {}

    public async Task<GuildGreeterConfigModel?> GetLatest(ulong guildId)
    {
        var collection = GetCollection();
        var filter = Builders<GuildGreeterConfigModel>
            .Filter
            .Eq("GuildId", guildId);
        var res = await collection.FindAsync(filter);
        var sorted = res.ToList().OrderByDescending(v => v.ModifiedAtTimestamp);
        return sorted.FirstOrDefault();
    }

    public async Task Add(GuildGreeterConfigModel model)
    {
        model._id = default;
        model.ModifiedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var collection = GetCollection();
        await collection.InsertOneAsync(model);
    }
}