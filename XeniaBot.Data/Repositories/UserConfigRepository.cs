using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Repositories;

[XeniaController]
public class UserConfigRepository : BaseRepository<UserConfigModel>
{
    public UserConfigRepository(IServiceProvider services)
        : base(UserConfigModel.CollectionName, services)
    {
        var collection = GetCollection();
        if (collection == null)
        {
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        }
        var existingIndexes = collection.Indexes.List().ToList().Count;
        if (existingIndexes < 1)
        {
            var keys = Builders<UserConfigModel>
                .IndexKeys
                .Descending("UserId")
                .Descending("ModifiedAtTimestamp");
            var indexModel = new CreateIndexModel<UserConfigModel>(keys);
            collection.Indexes.CreateOne(indexModel);
            Log.WriteLine($"Created Index");
        }
    }

    public async Task<UserConfigModel?> Get(ulong? id)
    {
        if (id == null)
        {
            return new UserConfigModel();
        }

        var filter = Builders<UserConfigModel>
            .Filter
            .Where(e => e.UserId == id);
        var sort = Builders<UserConfigModel>
            .Sort
            .Descending(e => e.ModifiedAtTimestamp);

        var result = await BaseFind(filter, sort, limit: 1);
        return result.FirstOrDefault();
    }

    public async Task<UserConfigModel> GetOrDefault(ulong id)
    {
        var res = await Get(id);
        return res
            ?? new UserConfigModel()
            {
                UserId = id
            };
    }

    public async Task Add(UserConfigModel model)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        model.ModifiedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        model.Id = default;
        await collection.InsertOneAsync(model);
    }
}