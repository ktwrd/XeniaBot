using XeniaBot.DiscordCache.Controllers;
using XeniaBot.DiscordCache.Models;
using XeniaBot.Shared;

namespace XeniaBot.DiscordCache.Repositories;

[XeniaController]
public class MessageCacheRepository : DiscordCacheGenericRepository<CacheMessageModel>
{
    public MessageCacheRepository(IServiceProvider services)
        : base(CacheMessageModel.CollectionName, services)
    {}
}