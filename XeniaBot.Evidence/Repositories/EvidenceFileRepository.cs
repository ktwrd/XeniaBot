using MongoDB.Driver;
using XeniaBot.Evidence.Models;
using XeniaBot.Shared;

namespace XeniaBot.Evidence.Repositories;

[XeniaController]
public class EvidenceFileRepository : BaseRepository<EvidenceFileModel>
{
    public EvidenceFileRepository(IServiceProvider services)
        : base(EvidenceFileModel.CollectionName, services)
    {
        
    }

    /// <summary>
    /// Get a document by <see cref="EvidenceFileModel.Id"/>
    /// </summary>
    public async Task<EvidenceFileModel?> Get(string id, bool includeDeleted = false)
    {
        var filter = Builders<EvidenceFileModel>
            .Filter
            .Where(v => v.Id == id && v.IsDeleted == false);
        if (includeDeleted)
        {
            filter = Builders<EvidenceFileModel>
                .Filter
                .Where(v => v.Id == id);
        }
        var res = await BaseFind(filter);
        return res.FirstOrDefault();
    }

    /// <inheritdoc cref="Get(string, bool)"/>
    public Task<EvidenceFileModel?> Get(EvidenceFileModel model, bool includeDeleted = false) => Get(model.Id, includeDeleted);

    /// <summary>
    /// <para>Insert a model into the collection.</para>
    /// 
    /// <para><see cref="EvidenceFileModel.Timestamp"/> will be updated.</para>
    /// </summary>
    /// <returns>Updated model</returns>
    public async Task<EvidenceFileModel> InsertOrUpdate(EvidenceFileModel model)
    {
        var filter = Builders<EvidenceFileModel>
            .Filter
            .Where(v => v.Id == model.Id);
        var collection = GetCollection();
        var existsRes = await BaseFind(filter);
        model.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (await existsRes.AnyAsync())
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