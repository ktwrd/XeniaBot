using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Repositories;

[XeniaController]
public class LevelMemberRepository : BaseRepository<LevelMemberModel>
{
    public LevelMemberRepository(IServiceProvider services)
        : base(LevelMemberModel.CollectionName, services)
    {}
    
    public async Task<LevelMemberModel?> Get(ulong userId, ulong guildId)
    {
        var filter = Builders<LevelMemberModel>
            .Filter
            .Where(v => v.UserId == userId && v.GuildId == guildId);
        var res = await BaseFind(filter, limit: 1);
        return res.FirstOrDefault();
    }
    public async Task<ICollection<LevelMemberModel>?> GetAllUsersCombined()
    {
        var filter = Builders<LevelMemberModel>
            .Filter.Empty;
        var result = await BaseFind(filter);
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
    public async Task<ICollection<LevelMemberModel>> GetGuild(ulong guildId)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var filter = Builders<LevelMemberModel>
            .Filter
            .Where(v => v.GuildId == guildId);

        var result = await BaseFind(filter);
        return await result.ToListAsync();
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
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
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
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
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