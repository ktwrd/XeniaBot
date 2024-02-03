using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Controllers.BotAdditions;

[XeniaController]
public class ReminderConfigController : BaseConfigController<ReminderModel>
{
    public ReminderConfigController(IServiceProvider services)
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

    public async Task<ReminderModel[]?> GetMany(
        long beforeTimestamp = long.MaxValue,
        long afterTimestamp = long.MinValue,
        bool hasReminded = false)
    {
        var filter = Builders<ReminderModel>
            .Filter
            .Where((v) => v.ReminderTimestamp < beforeTimestamp && v.ReminderTimestamp > afterTimestamp && v.HasReminded == hasReminded);
        return await InternalFindMany(filter);
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
}