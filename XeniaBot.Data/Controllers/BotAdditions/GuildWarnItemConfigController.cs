using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Controllers.BotAdditions;

[BotController]
public class GuildWarnItemConfigController : BaseConfigController<GuildWarnItemModel>
{
    public GuildWarnItemConfigController(IServiceProvider services)
        : base(GuildWarnItemModel.CollectionName, services)
    {}

    
    public async Task<GuildWarnItemModel?> GetLatest(string id)
    {
        var res = await GetItemsById(id);
        return res?.FirstOrDefault();
    }

    public async Task<ICollection<GuildWarnItemModel>?> GetItemsById(string id)
    {
        var collection = GetCollection();
        var filter = Builders<GuildWarnItemModel>
            .Filter
            .Eq("WarnId", id);
        var res = await collection.FindAsync(filter);
        var sorted = res.ToList().OrderByDescending(v => v.ModifiedAtTimestamp);
        return sorted.ToList();
    }

    public async Task<ICollection<GuildWarnItemModel>?> GetLatestGuildItems(ulong guildId)
    {
        var collection = GetCollection();
        var filter = Builders<GuildWarnItemModel>
            .Filter
            .Eq("GuildId", guildId);
        var res = await collection.FindAsync(filter);
        var parentList = res.ToList();
        var resultDict = new Dictionary<string, GuildWarnItemModel>();
        foreach (var item in parentList)
        {
            if (resultDict.TryGetValue(item.WarnId, out var r))
            {
                if (item.ModifiedAtTimestamp > r.ModifiedAtTimestamp)
                    resultDict[item.WarnId] = item;
            }
            else
            {
                resultDict.Add(item.WarnId, item);
            }
        }

        return resultDict.Select(v => v.Value).ToList();
    }
    
    public async Task Add(GuildWarnItemModel model)
    {
        var collection = GetCollection();
        model._id = default;
        model.ModifiedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await collection.InsertOneAsync(model);
    }
}