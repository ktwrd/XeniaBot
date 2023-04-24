using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using SkidBot.Shared;

namespace SkidBot.Core.Controllers.Wrappers.BigBrother;

public class BigBrotherGenericConfigController<T> : BaseConfigController<T> where T : BigBrotherBaseModel
{
    public BigBrotherGenericConfigController(string collectionName, IServiceProvider services)
        : base(collectionName, services)
    {
    }

    public async Task<T?> Get(ulong snowflake)
    {
        var collection = GetCollection();
        var filter = Builders<T>
            .Filter
            .Eq("Snowflake", snowflake);

        var result = await collection.FindAsync(filter);
        return result.FirstOrDefault();
    }

    public async Task Set(T model)
    {
        var collection = GetCollection();
        var filter = Builders<T>
            .Filter
            .Eq("Snowflake", model.Snowflake);

        var findResult = await collection.FindAsync(filter);
        if (findResult.Any())
        {
            await collection.ReplaceOneAsync(filter, model);
        }
        else
        {
            await collection.InsertOneAsync(model);
        }
    }
}