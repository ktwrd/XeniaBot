using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Shared;
using System.Diagnostics;
using XeniaBot.Data.Models;

namespace XeniaBot.Data.Controllers.BotAdditions
{
    [BotController]
    public class BanSyncConfigController : BaseConfigController<ConfigBanSyncModel>
    {
        public BanSyncConfigController(IServiceProvider services)
            : base("banSyncGuildConfig", services)
        {
        }

        public async Task<bool> Exists(ulong guildId)
        {
            var collection = GetCollection();
            var result = await collection.FindAsync(GetFilter(guildId));
            return result.Any();
        }
        public async Task<ConfigBanSyncModel> Get(ulong guildId)
        {
            var collection = GetCollection();
            var result = await collection.FindAsync(GetFilter(guildId));

            return result.FirstOrDefault();
        }
        public async Task Set(ConfigBanSyncModel data)
        {
            ulong guildId = data.GuildId;
            var collection = GetCollection();
            var filter = GetFilter(guildId);
            if (await Exists(guildId))
            {
                await collection.ReplaceOneAsync(filter, data);
            }
            else
            {
                await collection.InsertOneAsync(data);
            }
        }
        protected FilterDefinition<ConfigBanSyncModel> GetFilter(ulong guildId)
        {
            var filter = Builders<ConfigBanSyncModel>
                .Filter
                .Eq("GuildId", guildId);
            return filter;
        }
    }
}
