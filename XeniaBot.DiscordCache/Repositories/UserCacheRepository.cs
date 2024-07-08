using XeniaBot.DiscordCache.Controllers;
using XeniaBot.DiscordCache.Models;
using XeniaBot.Shared;

namespace XeniaBot.DiscordCache.Repositories;

[XeniaController]
public class UserCacheRepository : DiscordCacheGenericRepository<CacheUserModel>
{
    public UserCacheRepository(IServiceProvider services)
        : base(CacheUserModel.CollectionName, services)
    {}
}