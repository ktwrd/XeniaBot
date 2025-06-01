using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;
using XeniaBot.Shared;
using XeniaBot.Data.Models;
using System.Data;

namespace XeniaBot.Data.Repositories;

[XeniaController]
public class BanSyncConfigRepository : BaseRepository<ConfigBanSyncModel>
{
    private readonly BanSyncStateHistoryRepository _stateHistory;
    public BanSyncConfigRepository(IServiceProvider services)
        : base("banSyncGuildConfig", services)
    {
        _stateHistory = services.GetRequiredService<BanSyncStateHistoryRepository>();
    }

    public async Task<bool> Exists(ulong guildId)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var result = await collection.CountDocumentsAsync(GetFilter(guildId));
        return result > 0;
    }
    public async Task<ConfigBanSyncModel?> Get(ulong guildId)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var result = await BaseFind(GetFilter(guildId), limit: 1);

        return result.FirstOrDefault();
    }
    public async Task Set(ConfigBanSyncModel data)
    {
        var guildId = data.GuildId;
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var filter = GetFilter(guildId);
        if (await Exists(guildId))
        {
            await collection.ReplaceOneAsync(filter, data);
        }
        else
        {
            await collection.InsertOneAsync(data);
        }
        
        
        var stateHistoryItem = new BanSyncStateHistoryItemModel()
        {
            GuildId = data.GuildId,
            Enable = data.Enable,
            State = data.State,
            Reason = data.Reason
        };
        await _stateHistory.Add(stateHistoryItem);
    }
    protected FilterDefinition<ConfigBanSyncModel> GetFilter(ulong guildId)
    {
        var filter = Builders<ConfigBanSyncModel>
            .Filter
            .Where(e => e.GuildId == guildId);
        return filter;
    }
}
