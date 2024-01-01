using MongoDB.Driver;
using XeniaBot.Data.DbContexts;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Controllers;

[BotController]
public class UserConfigController : BaseConfigController<UserConfigModel>
{
    private readonly UserConfigDbContext _context;
    public UserConfigController(IServiceProvider services)
        : base(UserConfigModel.CollectionName, services)
    {
        _context = UserConfigDbContext.Create(_db);
    }

    public async Task<UserConfigModel?> Get(ulong? id)
    {
        if (id == null)
        {
            return new UserConfigModel();
        }

        var sorted = _context.Users
            .Where(v => v.UserId == id)
            .OrderByDescending(v => v.ModifiedAtTimestamp);
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
        model._id = default;
        model.ModifiedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        _context.Users.Add(model);
        _context.ChangeTracker.DetectChanges();
        await _context.SaveChangesAsync();
    }
}