using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Repositories;

[XeniaController]
public class LevelMemberRepository : BaseConfigController<LevelMemberModel>
{
    public LevelMemberRepository(IServiceProvider services)
        : base(LevelMemberModel.CollectionName, services)
    {}

    
    protected async Task<IAsyncCursor<LevelMemberModel>?> InternalFind(FilterDefinition<LevelMemberModel> filter)
    {
        var collection = GetCollection();
        var result = await collection.FindAsync(filter);
        return result;
    }
    public async Task<LevelMemberModel?> Get(ulong userId, ulong guildId)
    {
        var filter = MongoDB.Driver.Builders<LevelMemberModel>
            .Filter
            .Where(v => v.UserId == userId && v.GuildId == guildId);
        var res = await InternalFind(filter);
        return res.FirstOrDefault();
    }
    public async Task<ICollection<LevelMemberModel>?> GetAllUsersCombined()
    {
        var filter = Builders<LevelMemberModel>
            .Filter.Empty;
        var result = await InternalFind(filter);
        var data = new Dictionary<ulong, LevelMemberModel>();
        foreach (var item in result.ToEnumerable())
        {
            data.TryAdd(item.UserId, new LevelMemberModel()
            {
                UserId = item.UserId
            });
            data[item.UserId].Xp += item.Xp;
        }

        return data.Select(v => v.Value).ToList();
    }
    public async Task<LevelMemberModel[]?> GetGuild(ulong guildId)
    {
        var collection = GetCollection();
        var filter = Builders<LevelMemberModel>
            .Filter
            .Where(v => v.GuildId == guildId);

        var result = await collection.FindAsync(filter);
        var item = await result.ToListAsync();
        return item.ToArray();
    }
    /// <summary>
    /// Delete many objects from the database
    /// </summary>
    /// <returns>Amount of items deleted</returns>
    public async Task<long> Delete(ulong? user=null, ulong? guild=null, Func<ulong, bool>? xpFilter=null)
    {
        Func<LevelMemberModel, bool> filterFunction = (model) =>
        {
            int found = 0;
            int required = 0;

            required += user == null ? 0 : 1;
            required += guild == null ? 0 : 1;
            required += xpFilter == null ? 0 : 1;

            found += user == model.UserId ? 1 : 0;
            found += guild == model.GuildId ? 1 : 0;
            if (xpFilter != null)
            {
                found += xpFilter(model.Xp) ? 1 : 0;
            }

            return found >= required;
        };

        var filter = Builders<LevelMemberModel>
            .Filter
            .Where(v => filterFunction(v));

        var collection = GetCollection();
        var count = await collection.CountDocumentsAsync(filter);
        if (count < 1)
            return count;

        await collection.DeleteManyAsync(filter);
        return count;
    }
    public async Task Set(LevelMemberModel model)
    {
        var filter = Builders<LevelMemberModel>
            .Filter
            .Where(v => v.UserId == model.UserId && v.GuildId == model.GuildId);
        var exists = (await Get(model.UserId, model.GuildId)) != null;

        var collection = GetCollection();
        // Replace if exists, if not then we just insert
        if (exists)
        {
            await collection.ReplaceOneAsync(filter, model);
        }
        else
        {
            await collection.InsertOneAsync(model);
        }
    }
}