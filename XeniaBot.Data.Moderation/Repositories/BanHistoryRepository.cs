using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Data.Moderation.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Moderation.Repositories;

[XeniaController]
public class BanHistoryRepository : BaseRepository<BanHistoryModel>
{
    public BanHistoryRepository(IServiceProvider services)
        : base(BanHistoryModel.CollectionName, services)
    { }

    private SortDefinition<BanHistoryModel> BaseSort()
    {
        return Builders<BanHistoryModel>.Sort.Descending(v => v.Timestamp)!;
    }

    public async Task<BanHistoryModel?> GetLatest(ulong guildId, ulong userId)
    {
        var filter = Builders<BanHistoryModel>
            .Filter
            .Where(v => v.GuildId == guildId.ToString() && v.UserId == userId.ToString());
        var res = await BaseFind(filter, BaseSort());
        return res.FirstOrDefault();
    }

    public async Task<List<BanHistoryModel>?> GetMany(ulong guildId, ulong userId)
    {
        var filter = Builders<BanHistoryModel>
            .Filter
            .Where(v => v.GuildId == guildId.ToString() && v.UserId == userId.ToString());
        var res = await BaseFind(filter, BaseSort());
        return res.ToList();
    }

    /// <summary>
    /// Get all ban records for user. Will be sorted by <see cref="BanHistoryModel.Timestamp"/> in descending order.
    /// </summary>
    /// <param name="userId">User Id to fetch the ban records for.</param>
    public async Task<List<BanHistoryModel>?> GetManyUser(ulong userId)
    {
        var filter = Builders<BanHistoryModel>
            .Filter
            .Where(v => v.UserId == userId.ToString());
        var res = await BaseFind(filter, BaseSort());
        return res.ToList();
    }
    /// <summary>
    /// Get all bans in guild. Will be sorted by <see cref="BanHistoryModel.Timestamp"/> in descending order.
    /// </summary>
    /// <param name="guildId">Guild Id to filter the bans by.</param>
    public async Task<List<BanHistoryModel>?> GetManyInGuild(ulong guildId)
    {
        var filter = Builders<BanHistoryModel>
            .Filter
            .Where(v => v.GuildId == guildId.ToString());
        var res = await BaseFind(filter, BaseSort());
        return res.ToList();
    }

    /// <summary>
    /// Converts a list of records into a dict, which is mapped via the User Id.
    /// </summary>
    public Dictionary<ulong, List<BanHistoryModel>> MapKeyToUser(List<BanHistoryModel> items)
    {
        var dict = new Dictionary<ulong, List<BanHistoryModel>>();
        items = items.OrderByDescending(v => v.Timestamp).ToList();
        foreach (var item in items)
        {
            if (!dict.ContainsKey(item.GetUserId()))
                dict.Add(item.GetUserId(), new List<BanHistoryModel>());

            dict[item.GetUserId()].Add(item);
        }

        return dict;
    }

    /// <summary>
    /// Insert or Update a record based off <see cref="BanHistoryModel.Id"/>
    /// </summary>
    /// <param name="model">Model to replace with.</param>
    public async Task<BanHistoryModel> InsertOrUpdate(BanHistoryModel model)
    {
        var collection = GetCollection();
        var filter = Builders<BanHistoryModel>.Filter.Where(v => v.Id == model.Id);
        model.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var anyResult = await collection.FindAsync(filter);
        if (anyResult.Any())
        {
            await collection.ReplaceOneAsync(filter, model);
        }
        else
        {
            await collection.InsertOneAsync(model);
        }

        return model;
    }
}
