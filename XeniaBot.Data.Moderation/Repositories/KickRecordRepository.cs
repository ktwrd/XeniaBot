using MongoDB.Driver;
using XeniaBot.Data.Moderation.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Moderation.Repositories;

[XeniaController]
public class KickRecordRepository : BaseRepository<KickRecordModel>
{
    public KickRecordRepository(IServiceProvider services)
        : base(KickRecordModel.CollectionName, services)
    { }

    public async Task<KickRecordModel?> GetLatest(ulong guildId, ulong userId)
    {
        var filter = Builders<KickRecordModel>
            .Filter
            .Where(v => v.GuildId == guildId && v.UserId == userId);
        var order = Builders<KickRecordModel>.Sort.Descending(v => v.CreatedAt);
        var res = await BaseFind(filter, order);
        return res.FirstOrDefault();
    }

    /// <summary>
    /// Get all ban records for user. Will be sorted by <see cref="KickRecordModel.CreatedAt"/> in descending order.
    /// </summary>
    /// <param name="userId">User Id to fetch the ban records for.</param>
    public async Task<List<KickRecordModel>?> GetManyUser(ulong userId)
    {
        var filter = Builders<KickRecordModel>
            .Filter
            .Where(v => v.UserId == userId);
        var order = Builders<KickRecordModel>.Sort.Descending(v => v.CreatedAt);
        var res = await BaseFind(filter, order);
        return res.ToList();
    }
    /// <summary>
    /// Get all bans in guild. Will be sorted by <see cref="KickRecordModel.CreatedAt"/> in descending order.
    /// </summary>
    /// <param name="guildId">Guild Id to filter the bans by.</param>
    public async Task<List<KickRecordModel>?> GetManyInGuild(ulong guildId)
    {
        var filter = Builders<KickRecordModel>
            .Filter
            .Where(v => v.GuildId == guildId);
        var order = Builders<KickRecordModel>.Sort.Descending(v => v.CreatedAt);
        var res = await BaseFind(filter, order);
        return res.ToList();
    }
    /// <summary>
    /// Converts a list of records into a dict, which is mapped via the User Id.
    /// </summary>
    public Dictionary<ulong, List<KickRecordModel>> MapKeyToUser(List<KickRecordModel> items)
    {
        var dict = new Dictionary<ulong, List<KickRecordModel>>();
        items = items.OrderByDescending(v => v.CreatedAt).ToList();
        foreach (var item in items)
        {
            if (!dict.ContainsKey(item.UserId))
                dict.Add(item.UserId, new List<KickRecordModel>());

            dict[item.UserId].Add(item);
        }

        return dict;
    }

    /// <summary>
    /// Insert or Update a record based off <see cref="KickRecordModel.Id"/>
    /// </summary>
    /// <param name="model">Model to replace with.</param>
    /// <returns>Updated model.</returns>
    public async Task<KickRecordModel> InsertOrUpdate(KickRecordModel model)
    {
        var collection = GetCollection();
        var filter = Builders<KickRecordModel>.Filter.Where(v => v.Id == model.Id);

        var anyResult = await collection.FindAsync(filter);
        model.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
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