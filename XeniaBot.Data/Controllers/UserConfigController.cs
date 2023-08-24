using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Controllers;

[BotController]
public class UserConfigController : BaseConfigController<UserConfigModel>
{
    public UserConfigController(IServiceProvider services)
        : base("userConfig", services)
    {
    }

    public async Task<UserConfigModel?> Get(ulong? id)
    {
        if (id == null)
        {
            return new UserConfigModel();
        }
        var filter = Builders<UserConfigModel>
            .Filter
            .Eq("UserId", id);
        var collection = GetCollection();
        var result = await collection.FindAsync(filter);
        var sorted = result.ToList().OrderByDescending(v => v.ModifiedAtTimestamp);
        return sorted.FirstOrDefault();
    }

    public async Task<UserConfigModel> GetOrDefault(ulong id)
    {
        var res = await Get(id);
        return res
            ?? new UserConfigModel()
            {
                UserId = id
            };
    }

    public async Task Add(UserConfigModel model)
    {
        var collection = GetCollection();
        await collection.InsertOneAsync(model);
    }
}