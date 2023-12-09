using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Controllers.BotAdditions;

[BotController]
public class RoleMenuConfigController : BaseConfigController<RoleMenuConfigModel>
{
    public RoleMenuConfigController(IServiceProvider services)
        : base(RoleMenuConfigModel.CollectionName, services)
    {}

    
    public async Task<RoleMenuConfigModel?> GetLatest(string id)
    {
        var collection = GetCollection();
        var filter = Builders<RoleMenuConfigModel>
            .Filter
            .Eq("RoleMenuId", id);
        var res = await collection.FindAsync(filter);
        var sorted = res.ToList().OrderByDescending(v => v.ModifiedAtTimestamp);
        return sorted.FirstOrDefault();
    }

    public async Task<RoleMenuConfigModel?> GetLatestByMessageId(ulong messageId)
    {
        var collection = GetCollection();
        var filter = Builders<RoleMenuConfigModel>
            .Filter
            .Eq("MessageId", messageId);
        var res = await collection.FindAsync(filter);
        var sorted = res.ToList().OrderByDescending(v => v.ModifiedAtTimestamp);
        return sorted.FirstOrDefault();
    }

    public async Task<List<RoleMenuConfigModel>?> GetLatestInGuild(ulong guildId)
    {
        var collection = GetCollection();
        var dict = new Dictionary<string, RoleMenuConfigModel>();
        var filter = Builders<RoleMenuConfigModel>
            .Filter
            .Eq("GuildId", guildId);
        var res = await collection.FindAsync(filter);
        foreach (var item in res.ToList())
        {
            if (dict.TryGetValue(item.RoleMenuId, out var e))
            {
                if (e.ModifiedAtTimestamp < item.ModifiedAtTimestamp)
                    dict[item.RoleMenuId] = item;
            }
            else
            {
                dict.Add(item.RoleMenuId, item);
            }
        }

        return dict.Select(v => v.Value).ToList();
    }
    
    public async Task Add(RoleMenuConfigModel model)
    {
        var collection = GetCollection();
        model._id = default;
        model.ModifiedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await collection.InsertOneAsync(model);
    }
}