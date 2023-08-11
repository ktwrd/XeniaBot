using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using XeniaBot.Shared;

namespace XeniaBot.Core.Controllers.Wrappers.Archival;

public class ArchivalGenericConfigController<T> : BaseConfigController<T> where T : ArchiveBaseModel
{
    public ArchivalGenericConfigController(string collectionName, IServiceProvider services)
        : base(collectionName, services)
    {
    }

    public delegate void ModelSetDelegate(T? current, T? previous, bool isNewEntry);

    public event ModelSetDelegate OnModelSet;
    
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
        var first = findResult.FirstOrDefault();
        if (first != null)
        {
            await collection.DeleteManyAsync(filter);
        }
        await collection.InsertOneAsync(model);

        if (OnModelSet != null)
        {
            OnModelSet?.Invoke(model, first, first == null);
        }
    }
}