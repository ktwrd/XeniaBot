using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Controllers.BotAdditions;

[BotController]
public class RoleMenuOptionConfigController : BaseConfigController<RoleMenuOptionConfigModel>
{
    public RoleMenuOptionConfigController(IServiceProvider services)
        : base(RoleMenuOptionConfigModel.CollectionName, services)
    {}

    
    public async Task<RoleMenuOptionConfigModel?> GetLatest(string id)
    {
        var collection = GetCollection();
        var filter = Builders<RoleMenuOptionConfigModel>
            .Filter
            .Eq("RoleOptionId", id);
        var res = await collection.FindAsync(filter);
        var sorted = res.ToList().OrderByDescending(v => v.ModifiedAtTimestamp);
        return sorted.FirstOrDefault();
    }
    public async Task<List<RoleMenuOptionConfigModel>?> GetAll(string id)
    {
        var collection = GetCollection();
        var filter = Builders<RoleMenuOptionConfigModel>
            .Filter
            .Eq("RoleOptionId", id);
        var res = await collection.FindAsync(filter);
        var sorted = res.ToList().OrderByDescending(v => v.ModifiedAtTimestamp);
        return sorted.ToList();
    }

    public async Task<List<RoleMenuOptionConfigModel>?> GetLatestInSelect(string selectId)
    {
        var latestDict = new Dictionary<string, RoleMenuOptionConfigModel>();
        var collection = GetCollection();
        var filter = Builders<RoleMenuOptionConfigModel>
            .Filter
            .Eq("RoleSelectId", selectId);
        var res = await collection.FindAsync(filter);
        foreach (var item in res.ToList())
        {
            if (latestDict.ContainsKey(item.RoleOptionId))
            {
                if (latestDict[item.RoleOptionId].ModifiedAtTimestamp < item.ModifiedAtTimestamp)
                    latestDict[item.RoleOptionId] = item;
            }
            else
            {
                latestDict.Add(item.RoleOptionId, item);
            }
        }

        return latestDict.Select(v => v.Value).ToList();
    }

    public async Task Delete(string roleOptionId, long? beforeTimestamp = null, long? afterTimestamp = null)
    {
        var all = await GetAll(roleOptionId);
        var collection = GetCollection();
        if (beforeTimestamp == null && afterTimestamp == null)
        {
            await collection.DeleteManyAsync(
                Builders<RoleMenuOptionConfigModel>.Filter.Eq("RoleOptionId", roleOptionId));
            return;
        }
        foreach (var item in all)
        {
            if (beforeTimestamp != null)
            {
                if (beforeTimestamp > item.ModifiedAtTimestamp)
                    collection.DeleteOneAsync(Builders<RoleMenuOptionConfigModel>.Filter.Eq("_id", item._id));
            }
            else if (afterTimestamp != null)
            {
                if (afterTimestamp < item.ModifiedAtTimestamp)
                    collection.DeleteOneAsync(Builders<RoleMenuOptionConfigModel>.Filter.Eq("_id", item._id));
            }
        }
    }
    
    public async Task Add(RoleMenuOptionConfigModel model)
    {
        var collection = GetCollection();
        model._id = default;
        model.ModifiedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await collection.InsertOneAsync(model);
    }
}