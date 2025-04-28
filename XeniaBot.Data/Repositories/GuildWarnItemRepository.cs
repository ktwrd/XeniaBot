using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Repositories;

[XeniaController]
public class GuildWarnItemRepository : BaseRepository<GuildWarnItemModel>
{
    public GuildWarnItemRepository(IServiceProvider services)
        : base(GuildWarnItemModel.CollectionName, services)
    {}

    private SortDefinition<GuildWarnItemModel> sort_createdAt
        => Builders<GuildWarnItemModel>
            .Sort
            .Descending(v => v.CreatedAtTimestamp);
    private SortDefinition<GuildWarnItemModel> sort_modifiedAt
        => Builders<GuildWarnItemModel>
            .Sort
            .Descending(v => v.ModifiedAtTimestamp);
    
    public async Task<GuildWarnItemModel?> GetLatest(string id)
    {
        var res = await GetItemsById(id);
        return res?.FirstOrDefault();
    }

    public async Task<ICollection<GuildWarnItemModel>?> GetItemsById(string id)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
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
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
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

    public async Task<ICollection<GuildWarnItemModel>?> GetLatestGuildMemberItems(ulong guildId, ulong userId, long createdAfter = 0)
    {
        var filter = Builders<GuildWarnItemModel>
            .Filter
            .Where(v => v.GuildId == guildId && v.TargetUserId == userId);
        var workingRes = await BaseFind(filter);
        var data = workingRes.ToList()
            .OrderByDescending(v => v.CreatedAtTimestamp)
            .ThenBy(v => v.WarnId)
            .ThenByDescending(v => v.ModifiedAtTimestamp)
            .GroupBy(v => v.WarnId)
            .Select(v => v.First())
            .Where(v => v.CreatedAtTimestamp > createdAfter);
        return data.ToList();
    }
    
    public async Task Add(GuildWarnItemModel model)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        model.ResetId();
        model.ModifiedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await collection.InsertOneAsync(model);
    }
}