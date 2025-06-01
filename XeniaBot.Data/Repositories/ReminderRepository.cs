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
public class ReminderRepository : BaseRepository<ReminderModel>
{
    public ReminderRepository(IServiceProvider services)
        : base("reminderConfig", services)
    {
    }

    public async Task<ReminderModel?> Get(string reminderId)
    {
        var filter = Builders<ReminderModel>
            .Filter
            .Where(e => e.ReminderId == reminderId);
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var res = await collection.FindAsync(filter);
        var single = res.FirstOrDefault();
        return single;
    }

    // public async Task<ReminderModel[]?> GetMany(
    //     long beforeTs = long.MaxValue,
    //     long afterTs = long.MinValue,
    //     ulong? authorId = null,
    //     ulong? guildId = null,
    //     ulong? channelId = null,
    //     bool? hasReminded = null)
    // {
    //     var filter = Builders<ReminderModel>
    //         .Filter
    //         .Where((m) =>
    //             beforeTs > m.ReminderTimestamp &&
    //             afterTs < m.ReminderTimestamp &&
    //             m.UserId == (authorId ?? m.UserId) &&
    //             m.GuildId == (guildId ?? m.GuildId) &&
    //             m.ChannelId == (channelId ?? m.ChannelId) &&
    //             m.HasReminded == (hasReminded ?? m.HasReminded));
    //     var collection = GetCollection();
    //     var result = await collection.FindAsync(filter);
    //     
    //     var final = result?.ToList().ToArray();
    //     return final;
    // }

    public async Task<List<ReminderModel>?> GetMany(
        long beforeTimestamp = long.MaxValue,
        long afterTimestamp = long.MinValue,
        bool hasReminded = false)
    {
        var filter = Builders<ReminderModel>
            .Filter
            .Where((v) => v.ReminderTimestamp < beforeTimestamp && v.ReminderTimestamp > afterTimestamp && v.HasReminded == hasReminded);
        var res = await BaseFind(filter);
        return res.ToList();
    }

    private async Task<ReminderModel[]?> InternalFindMany(FilterDefinition<ReminderModel> filter)
    {
        var collection = GetCollection();
        var result = await collection.FindAsync(filter);
        
        var final = result?.ToList().ToArray();
        return final;
    }

    /// <summary>
    /// Get all documents
    /// </summary>
    public async Task<IAsyncCursor<ReminderModel>?> GetAll()
    {
        var filter = Builders<ReminderModel>
            .Filter
            .Empty;
        return await BaseFind(filter);
    }

    public async Task Set(ReminderModel model)
    {
        var filter = Builders<ReminderModel>
            .Filter
            .Where(e => e.ReminderId == model.ReminderId);
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var result = await collection.CountDocumentsAsync(filter);
        if (result > 0)
        {
            await collection.ReplaceOneAsync(filter, model);
        }
        else
        {
            await collection.InsertOneAsync(model);
        }
    }

    public async Task<List<ReminderModel>> GetForgotten(string[] currentItems, long initTimestamp)
    {
        var notCalled = await GetMany(
            beforeTimestamp: initTimestamp,
            hasReminded: false) ?? [];

        var results = new List<ReminderModel>();
        foreach (var i in notCalled)
        {
            if (!currentItems.Contains(i.ReminderId))
            {
                results.Add(i);
            }
        }

        return results;
    }

    public async Task<ICollection<ReminderModel>> GetByUser(ulong userId)
    {
        var filter = Builders<ReminderModel>
            .Filter
            .Where(e => e.UserId == userId);
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var results = await collection.FindAsync(filter);
        return results.ToList();
    }

    public async Task<ICollection<ReminderModel>> GetByUserPaginate(ulong userId, int page, int pageSize)
    {
        var filter = Builders<ReminderModel>
            .Filter
            .Where(v => v.UserId == userId && v.HasReminded == false);
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var result = await collection.Find(filter)
            .SortByDescending(v => v.ReminderTimestamp)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
        return result;
    }
}