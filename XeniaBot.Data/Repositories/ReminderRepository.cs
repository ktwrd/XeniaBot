using System;
using System.Collections.Generic;
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
            .Eq("ReminderId", reminderId);
        var res = await GetCollection().FindAsync(filter);
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

    public async Task Set(ReminderModel model)
    {
        var filter = Builders<ReminderModel>
            .Filter
            .Eq("ReminderId", model.ReminderId);
        var collection = GetCollection();
        var result = await collection.FindAsync(filter);
        var exists = await result.AnyAsync();
        if (exists)
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
            hasReminded: false) ?? Array.Empty<ReminderModel>();

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
            .Eq("UserId", userId);
        var collection = GetCollection();
        var results = await collection.FindAsync(filter);
        return results.ToList();
    }
}