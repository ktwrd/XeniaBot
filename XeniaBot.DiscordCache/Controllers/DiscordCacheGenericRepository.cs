using System.Data;
using MongoDB.Driver;
using XeniaBot.DiscordCache.Models;
using XeniaBot.Shared;

namespace XeniaBot.DiscordCache.Controllers;

public class DiscordCacheGenericRepository<T> : BaseRepository<T> where T : DiscordCacheBaseModel
{
    public DiscordCacheGenericRepository(string collectionName, IServiceProvider services)
        : base(collectionName, services)
    {
        var collection = GetCollection();
        if (collection == null)
        {
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        }

        var existingIndexes = collection.Indexes.List().ToList();
        var targetIndexes = new Dictionary<string, IndexKeysDefinition<T>>()
        {
            {
                collectionName + "_IX_SnowflakeModifiedAtTimestamp",
                Builders<T>
                    .IndexKeys
                    .Descending("Snowflake")
                    .Descending("ModifiedAtTimestamp")
            },
            {
                collectionName + "_IX_Snowflake",
                Builders<T>
                    .IndexKeys
                    .Descending("Snowflake")
            }
        };
        foreach (var (name, idx) in targetIndexes)
        {
            if (!existingIndexes.Any(e => e.GetElement("name").Value.AsString == name))
            {
                var model = new CreateIndexModel<T>(idx, new CreateIndexOptions()
                {
                    Name = name
                });
                try
                {
                    collection.Indexes.CreateOne(model);
                    Log.WriteLine($"{collectionName} - Created index \"{name}\"");
                }
                catch (Exception ex)
                {
                    Log.Error($"{collectionName} - Failed to create index \"{name}\"", ex);
                }
            }
        }
    }

    public delegate void ModelSetDelegate(T? current, T? previous, bool isNewEntry);
    public delegate void ModelAddDelegate(T? data);

    public event ModelSetDelegate? OnModelSet;
    public event ModelAddDelegate? OnModelAdd;

    public async Task<T?> Get(ulong snowflake)
    {
        var collection = GetCollection();
        var filter = Builders<T>
            .Filter
            .Where(e => e.Snowflake == snowflake);

        var result = await collection.FindAsync(filter);
        return result.FirstOrDefault();
    }

    /// <summary>
    /// Get latest (Selected by <see cref="DiscordCacheBaseModel.ModifiedAtTimestamp"/>) by a snowflake.
    /// </summary>
    /// <param name="snowflake">Snowflake of item to fetch</param>
    /// <returns>Latest item matching snowflake.</returns>
    public async Task<T?> GetLatest(ulong snowflake)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var filter = Builders<T>
            .Filter
            .Where(e => e.Snowflake == snowflake);

        var sort = Builders<T>
            .Sort
            .Descending(e => e.ModifiedAtTimestamp);

        var opts = new FindOptions<T>()
        {
            Limit = 1,
            Sort = sort
        };

        var result = await collection.FindAsync(filter, opts);
        return await result.FirstOrDefaultAsync();
    }

    /// <summary>
    /// Forcefully replace an item with matching <see cref="DiscordCacheBaseModel.ModifiedAtTimestamp"/> and <see cref="DiscordCacheBaseModel.Snowflake"/>.
    /// </summary>
    /// <param name="model">Model to replace existing entries with.</param>
    public async Task Set(T model)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var filter = Builders<T>
            .Filter
            .Where(v => v.Snowflake == model.Snowflake && v.ModifiedAtTimestamp == model.ModifiedAtTimestamp);
        var findOptions = new FindOptions<T>()
        {
            Limit = 1
        };

        var findResult = await collection.FindAsync(filter, findOptions);
        var first = findResult.FirstOrDefault();
        if (first != null)
        {
            await collection.DeleteManyAsync(filter);
        }
        await collection.InsertOneAsync(model);

        OnModelSet?.Invoke(model, first, first == null);
    }

    /// <summary>
    /// Add an item to the collection. Will set <see cref="DiscordCacheBaseModel.ModifiedAtTimestamp"/> to the current Epoch MS Timestamp.
    /// </summary>
    /// <param name="model">Model to add to the collection</param>
    public async Task Add(T model)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        model.ResetId();
        model.ModifiedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await collection.InsertOneAsync(model);
        OnModelAdd?.Invoke(model);
    }
}