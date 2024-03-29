using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace XeniaBot.Shared;

/// <summary>
/// Base controller that interacts with MongoDB
/// </summary>
/// <typeparam name="TH">Type of the target collection specified at <see cref="MongoCollectionName"/></typeparam>
public class BaseRepository<TH> : BaseService
{
    protected IMongoDatabase _db { get; private set; }
    /// <summary>
    /// Name of the MongoDB Collection
    /// </summary>
    public string MongoCollectionName { get; private set; }
    public BaseRepository(string collectionName, IServiceProvider services)
        : base(services)
    {
        _db = services.GetRequiredService<IMongoDatabase>();
        MongoCollectionName = collectionName;
    }

    /// <summary>
    /// Get specific collection with specific type.
    /// </summary>
    /// <param name="name">Name of collection to fetch</param>
    /// <typeparam name="T">Type of the collection</typeparam>
    /// <returns>MongoDB Collection or null <see cref="IMongoDatabase.GetCollection{TDocument}(string, MongoCollectionSettings)"/></returns>
    protected IMongoCollection<T>? GetCollection<T>(string name)
        => _db.GetCollection<T>(name);

    /// <summary>
    /// Get current collection (specified at <see cref="MongoCollectionName"/>) with custom type.
    /// </summary>
    /// <typeparam name="T">Custom document type.</typeparam>
    /// <returns>MongoDB Collection or null <see cref="IMongoDatabase.GetCollection{TDocument}(string, MongoCollectionSettings)"/></returns>
    protected IMongoCollection<T>? GetCollection<T>()
        => _db.GetCollection<T>(MongoCollectionName);

    /// <summary>
    /// Get collection with the name of <see cref="MongoCollectionName"/>
    /// </summary>
    /// <returns>MongoDB Collection with assumed type of <typeparamref name="TH"/> or null if the collection doesn't exist.</returns>
    protected IMongoCollection<TH>? GetCollection()
        => GetCollection<TH>();

    /// <summary>
    /// Get collection by <paramref name="name"/>
    /// </summary>
    /// <param name="name">Collection name to fetch</param>
    /// <returns>MongoDB Collection with assumed type of <typeparamref name="TH"/> or null if the collection doesn't exist.</returns>
    protected IMongoCollection<TH>? GetCollection(string name)
        => GetCollection<TH>(name);
    
    public Task<IAsyncCursor<TH>> BaseFind(FilterDefinition<TH> filter, SortDefinition<TH> sort = null)
    {
        var collection = GetCollection();
        var opts = new FindOptions<TH, TH>();
        if (sort != null)
            opts.Sort = sort;
        return collection.FindAsync(filter, opts);
    }
}