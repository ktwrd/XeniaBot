using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Controllers.BotAdditions;

[BotController]
public class GuildGreetByeConfigController : BaseConfigController<GuildByeGreeterConfigModel>
{
    public GuildGreetByeConfigController(IServiceProvider services)
        : base(GuildByeGreeterConfigModel.CollectionName, services)
    {}

    public async Task<GuildByeGreeterConfigModel?> GetLatest(ulong guildId)
    {
        var collection = GetCollection();
        var filter = Builders<GuildByeGreeterConfigModel>
            .Filter
            .Eq("GuildId", guildId);
        var res = await collection.FindAsync(filter);
        var sorted = res.ToList().OrderByDescending(v => v.ModifiedAtTimestamp);
        return sorted.FirstOrDefault();
    }

    public async Task Add(GuildByeGreeterConfigModel model)
    {
        model._id = default;
        model.ModifiedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var collection = GetCollection();
        await collection.InsertOneAsync(model);
    }
}