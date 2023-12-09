using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Controllers.BotAdditions;

[BotController]
public class RoleMenuSelectConfigController : BaseConfigController<RoleMenuSelectConfigModel>
{
    public RoleMenuSelectConfigController(IServiceProvider services)
        : base(RoleMenuSelectConfigModel.CollectionName, services)
    {}

    
    public async Task<RoleMenuSelectConfigModel?> GetLatest(string id)
    {
        var collection = GetCollection();
        var filter = Builders<RoleMenuSelectConfigModel>
            .Filter
            .Eq("RoleSelectId", id);
        var res = await collection.FindAsync(filter);
        var sorted = res.ToList().OrderByDescending(v => v.ModifiedAtTimestamp);
        return sorted.FirstOrDefault();
    }

    public async Task<List<RoleMenuSelectConfigModel>?> GetAll(string id)
    {
        var collection = GetCollection();
        var filter = Builders<RoleMenuSelectConfigModel>
            .Filter
            .Eq("RoleSelectId", id);
        var res = await collection.FindAsync(filter);
        var sorted = res.ToList().OrderByDescending(v => v.ModifiedAtTimestamp);
        return sorted.ToList();
    }

    public async Task<List<RoleMenuSelectConfigModel>?> GetLatestInMenu(string menuId)
    {
        var latestDict = new Dictionary<string, RoleMenuSelectConfigModel>();
        var collection = GetCollection();
        var filter = Builders<RoleMenuSelectConfigModel>
            .Filter
            .Eq("RoleMenuId", menuId);
        var res = await collection.FindAsync(filter);
        foreach (var item in res.ToList())
        {
            if (latestDict.ContainsKey(item.RoleSelectId))
            {
                if (latestDict[item.RoleSelectId].ModifiedAtTimestamp < item.ModifiedAtTimestamp)
                    latestDict[item.RoleSelectId] = item;
            }
            else
            {
                latestDict.Add(item.RoleSelectId, item);
            }
        }

        return latestDict.Select(v => v.Value).ToList();
    }

    /// <summary>
    /// Delete records from database.
    /// </summary>
    /// <param name="roleSelectId">RoleSelectId to delete</param>
    /// <param name="beforeTimestamp">Records to delete before <see cref="RoleMenuSelectConfigModel.ModifiedAtTimestamp"/></param>
    /// <param name="afterTimestamp">Records to delete after <see cref="RoleMenuSelectConfigModel.ModifiedAtTimestamp"/></param>
    public async Task Delete(string roleSelectId, long? beforeTimestamp = null, long? afterTimestamp = null)
    {
        var all = await GetAll(roleSelectId);
        var collection = GetCollection();
        if (beforeTimestamp == null && afterTimestamp == null)
        {
            await collection.DeleteManyAsync(
                Builders<RoleMenuSelectConfigModel>.Filter.Eq("RoleSelectId", roleSelectId));
            return;
        }
        foreach (var item in all)
        {
            if (beforeTimestamp != null)
            {
                if (beforeTimestamp > item.ModifiedAtTimestamp)
                    collection.DeleteOneAsync(Builders<RoleMenuSelectConfigModel>.Filter.Eq("_id", item._id));
            }
            else if (afterTimestamp != null)
            {
                if (afterTimestamp < item.ModifiedAtTimestamp)
                    collection.DeleteOneAsync(Builders<RoleMenuSelectConfigModel>.Filter.Eq("_id", item._id));
            }
        }
    }

    /// <summary>
    /// Delete single role matching <see cref="RoleMenuSelectConfigModel._id"/>.
    /// </summary>
    /// <param name="model">Role selection to delete</param>
    public async Task DeleteSingle(RoleMenuSelectConfigModel model)
    {
        var collection = GetCollection();
        var filter = Builders<RoleMenuSelectConfigModel>.Filter.Eq("_id", model._id);
        await collection.DeleteOneAsync(filter);
    }
    
    public async Task Add(RoleMenuSelectConfigModel model)
    {
        var collection = GetCollection();
        model._id = default;
        model.ModifiedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await collection.InsertOneAsync(model);
    }
}