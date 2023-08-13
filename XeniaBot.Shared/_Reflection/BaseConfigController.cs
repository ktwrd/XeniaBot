using System;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace XeniaBot.Shared;

public class BaseConfigController<TH> : BaseController
{
    protected IMongoDatabase _db;
    protected string MongoCollectionName { get; private set; }
    public BaseConfigController(string collectionName, IServiceProvider services)
        : base(services)
    {
        _db = services.GetRequiredService<IMongoDatabase>();
        MongoCollectionName = collectionName;
    }

    protected IMongoCollection<T>? GetCollection<T>(string name)
        => _db.GetCollection<T>(name);

    protected IMongoCollection<T>? GetCollection<T>()
        => _db.GetCollection<T>(MongoCollectionName);

    protected IMongoCollection<TH>? GetCollection()
        => GetCollection<TH>();

    protected IMongoCollection<TH>? GetCollection(string name)
        => GetCollection<TH>(name);

}