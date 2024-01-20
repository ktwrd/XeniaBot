using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Controllers.BotAdditions;

[BotController]
public class AttachmentArchiveConfigController : BaseConfigController<AttachmentArchiveModel>
{
    public AttachmentArchiveConfigController(IServiceProvider services)
        : base(AttachmentArchiveModel.CollectionName, services)
    {}
    
    public async Task<AttachmentArchiveModel?> Get(string id)
    {
        var collection = GetCollection();
        var filter = Builders<AttachmentArchiveModel>
            .Filter
            .Eq("ArchiveId", id);
        var res = await collection.FindAsync(filter);
        return res.FirstOrDefault();
    }
    public async Task Set(AttachmentArchiveModel model)
    {
        var collection = GetCollection();
        var filter = Builders<AttachmentArchiveModel>
            .Filter
            .Eq("ArchiveId", model.ArchiveId);

        var existResult = await collection.FindAsync(filter);
        var exists = await existResult.AnyAsync();

        if (exists)
        {
            await collection.DeleteManyAsync(filter);
        }
        await collection.InsertOneAsync(model);
    }
}