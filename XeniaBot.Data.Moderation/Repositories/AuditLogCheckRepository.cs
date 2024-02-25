using MongoDB.Driver;
using XeniaBot.Data.Moderation.Models;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;

namespace XeniaBot.Data.Moderation.Repositories;

[XeniaController]
public class AuditLogCheckRepository : BaseRepository<AuditLogCheckRecord>
{
    public AuditLogCheckRepository(IServiceProvider services)
        : base(AuditLogCheckRecord.CollectionName, services)
    { }

    public Task<AuditLogCheckRecord?> Get(ulong guildId, string actionType, Guid instanceId) =>
        Get(guildId, actionType, instanceId.ToString());
    
    public async Task<AuditLogCheckRecord?> Get(ulong guildId, string actionType, string instanceId)
    {
        var filter = Builders<AuditLogCheckRecord>
            .Filter
            .Where(v => v.GuildId == guildId && v.ActionType == actionType && v.InstanceId == instanceId);
        var sort = Builders<AuditLogCheckRecord>
            .Sort
            .Descending(v => v.Timestamp);
        var res = await BaseFind(filter, sort);
        return res.FirstOrDefault();
    }

    public async Task<AuditLogCheckRecord> Add(AuditLogCheckRecord model)
    {
        model.InstanceId = CoreContext.InstanceId.ToString();
        model.InsertTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var collection = GetCollection();
        await collection.InsertOneAsync(model);
        return model;
    }
}