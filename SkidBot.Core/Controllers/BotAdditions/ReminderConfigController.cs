using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using SkidBot.Core.Models;
using SkidBot.Shared;

namespace SkidBot.Core.Controllers.BotAdditions;

[SkidController]
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

    public async Task<ReminderModel[]?> GetMany(
        long? beforeTs = null,
        long? afterTs = null,
        ulong? authorId = null,
        ulong? guildId = null,
        ulong? channelId = null,
        bool? hasReminded = null)
    {
        Func<ReminderModel, bool> filterFunc = (m) =>
        {
            int count = 0;
            int requiredCount = 0;

            requiredCount += beforeTs != null ? 1 : 0;
            requiredCount += afterTs != null ? 1 : 0;
            requiredCount += authorId != null ? 1 : 0;
            requiredCount += guildId != null ? 1 : 0;
            requiredCount += channelId != null ? 1 : 0;
            requiredCount += hasReminded != null ? 1 : 0;

            if (beforeTs != null)
                count += m.ReminderTimestamp < beforeTs ? 1 : 0;
            if (afterTs != null)
                count += m.ReminderTimestamp > afterTs ? 1 : 0;
            count += m.UserId == authorId ? 1 : 0;
            count += m.GuildId == guildId ? 1 : 0;
            count += m.ChannelId == channelId ? 1 : 0;
            count += m.HasReminded == hasReminded ? 1 : 0;
            
            return count >= requiredCount;
        };

        var filter = Builders<ReminderModel>
            .Filter
            .Where(v => filterFunc(v));
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