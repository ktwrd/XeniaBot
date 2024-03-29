using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Repositories;

[XeniaController]
public class XeniaVersionRepository : BaseRepository<XeniaVersionModel>
{
    public XeniaVersionRepository(IServiceProvider services)
        : base(XeniaVersionModel.CollectionName, services)
    {
        Priority = 11;
    }

    private SortDefinition<XeniaVersionModel> sort_createdAt
        => Builders<XeniaVersionModel>
            .Sort
            .Descending(v => v.CreatedAt);

    public async Task<XeniaVersionModel?> Get(string id)
    {
        var filter = Builders<XeniaVersionModel>
            .Filter
            .Where(v => v.Id == id);
        var res = await BaseFind(filter);
        return res.FirstOrDefault();
    }
    public async Task<List<XeniaVersionModel>> GetAllByName(string name)
    {
        var filter = Builders<XeniaVersionModel>
            .Filter
            .Where(v => v.Name == name);
        var res = await BaseFind(filter, sort_createdAt);
        return res.ToList();
    }

    public async Task<XeniaVersionModel?> GetPrevious(string currentId)
    {
        var current = await Get(currentId);
        if (current == null)
            throw new NoNullAllowedException($"Failed to fetch current record {currentId}. Resulted in null");

        var filter = Builders<XeniaVersionModel>
            .Filter
            .Where(v => v.Name == current.Name && v.ParsedVersionTimestamp < current.ParsedVersionTimestamp);
        var res = await BaseFind(filter, sort_createdAt);
        return res.FirstOrDefault();
    }

    public async Task Insert(XeniaVersionModel model)
    {
        var collection = GetCollection();
        await collection.InsertOneAsync(model);
    }
}