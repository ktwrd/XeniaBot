using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using XeniaBot.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Shared;
using System.Diagnostics;

namespace XeniaBot.Core.Controllers.BotAdditions
{
    [BotController]
    public class BanSyncConfigController : BaseController
    {
        public BanSyncConfigController(IServiceProvider services)
            : base(services)
        {
        }

        public override Task InitializeAsync() => Task.CompletedTask;

        public const string MongoCollectionName = "banSyncGuildConfig";
        protected static IMongoCollection<T>? GetCollection<T>()
        {
            return Program.GetMongoDatabase()?.GetCollection<T>(MongoCollectionName);
        }
        protected static IMongoCollection<ConfigBanSyncModel>? GetCollection()
            => GetCollection<ConfigBanSyncModel>();

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
