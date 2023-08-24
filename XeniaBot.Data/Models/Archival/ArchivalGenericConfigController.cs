using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using XeniaBot.Shared;

namespace XeniaBot.Data.Models.Archival;

public class ArchivalGenericConfigController<T> : BaseConfigController<T> where T : ArchiveBaseModel
{
    public ArchivalGenericConfigController(string collectionName, IServiceProvider services)
        : base(collectionName, services)
    {
    }

    public delegate void ModelSetDelegate(T? current, T? previous, bool isNewEntry);
    public delegate void ModelAddDelegate(T? data);

    public event ModelSetDelegate OnModelSet;
    public event ModelAddDelegate OnModelAdd;
    
    public async Task<T?> Get(ulong snowflake)
    {
        var collection = GetCollection();
        var filter = Builders<T>
            .Filter
            .Eq("Snowflake", snowflake);

        var result = await collection.FindAsync(filter);
        return result.FirstOrDefault();
    }

    public async Task<T?> GetLatest(ulong snowflake)
    {
        var collection = GetCollection();
        var filter = Builders<T>
            .Filter
            .Eq("Snowflake", snowflake);
        
        var result = await collection.FindAsync(filter);
        var sorted = result.ToList().OrderByDescending(v => v.ModifiedAtTimestamp);
        return sorted.FirstOrDefault();
    }
    public async Task Set(T model)
    {
        var collection = GetCollection();
        var filter = Builders<T>
            .Filter
            .Where(v => v.Snowflake == model.Snowflake && v.ModifiedAtTimestamp == model.ModifiedAtTimestamp);

        var findResult = await collection.FindAsync(filter);
        var first = findResult.FirstOrDefault();
        if (first != null)
        {
            await collection.DeleteManyAsync(filter);
        }
        await collection.InsertOneAsync(model);

        OnModelSet?.Invoke(model, first, first == null);
    }
    public async Task Add(T model)
    {
        model.ModifiedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var collection = GetCollection();
        await collection.InsertOneAsync(model);
        OnModelAdd?.Invoke(model);
    }
}