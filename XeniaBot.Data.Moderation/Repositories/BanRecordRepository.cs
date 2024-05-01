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
public class BanRecordRepository : BaseRepository<BanRecordModel>
{
    public BanRecordRepository(IServiceProvider services)
        : base(BanRecordModel.CollectionName, services)
    { }

    public async Task<BanRecordModel?> GetLatest(ulong guildId, ulong userId)
    {
        var filter = Builders<BanRecordModel>
            .Filter
            .Where(v => v.GuildId == guildId.ToString() && v.UserId == userId.ToString());
        var order = Builders<BanRecordModel>.Sort.Descending(v => v.CreatedAt);
        var res = await BaseFind(filter, order);
        return res.FirstOrDefault();
    }

    /// <summary>
    /// Get all ban records for user. Will be sorted by <see cref="BanRecordModel.CreatedAt"/> in descending order.
    /// </summary>
    /// <param name="userId">User Id to fetch the ban records for.</param>
    public async Task<List<BanRecordModel>?> GetManyUser(ulong userId)
    {
        var filter = Builders<BanRecordModel>
            .Filter
            .Where(v => v.UserId == userId.ToString());
        var order = Builders<BanRecordModel>.Sort.Descending(v => v.CreatedAt);
        var res = await BaseFind(filter, order);
        return res.ToList();
    }
    /// <summary>
    /// Get all bans in guild. Will be sorted by <see cref="BanRecordModel.CreatedAt"/> in descending order.
    /// </summary>
    /// <param name="guildId">Guild Id to filter the bans by.</param>
    public async Task<List<BanRecordModel>?> GetManyInGuild(ulong guildId)
    {
        var filter = Builders<BanRecordModel>
            .Filter
            .Where(v => v.GuildId == guildId.ToString());
        var order = Builders<BanRecordModel>.Sort.Descending(v => v.CreatedAt);
        var res = await BaseFind(filter, order);
        return res.ToList();
    }
    /// <summary>
    /// Converts a list of records into a dict, which is mapped via the User Id.
    /// </summary>
    public Dictionary<ulong, List<BanRecordModel>> MapKeyToUser(List<BanRecordModel> items)
    {
        var dict = new Dictionary<ulong, List<BanRecordModel>>();
        items = items.OrderByDescending(v => v.Timestamp).ToList();
        foreach (var item in items)
        {
            if (!dict.ContainsKey(item.GetUserId()))
                dict.Add(item.GetUserId(), new List<BanRecordModel>());

            dict[item.GetUserId()].Add(item);
        }

        return dict;
    }

    /// <summary>
    /// Insert or Update a record based off <see cref="BanRecordModel.Id"/>
    /// </summary>
    /// <param name="model">Model to replace with.</param>
    /// <returns>Updated model.</returns>
    public async Task<BanRecordModel> InsertOrUpdate(BanRecordModel model)
    {
        var collection = GetCollection();
        var filter = Builders<BanRecordModel>.Filter.Where(v => v.Id == model.Id);

        var anyResult = await collection.FindAsync(filter);
        model.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
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