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
            .Eq("UserId", id);
        var collection = GetCollection();
        var result = await collection.FindAsync(filter);
        var sorted = result.ToList().OrderByDescending(v => v.ModifiedAtTimestamp);
        return sorted.FirstOrDefault();
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
        model.Id = default;
        model.ModifiedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var collection = GetCollection();
        await collection.InsertOneAsync(model);
    }
}