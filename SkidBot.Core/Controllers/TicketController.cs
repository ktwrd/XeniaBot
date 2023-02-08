using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using SkidBot.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Controllers
{
    public class TicketController
    {
        private readonly DiscordSocketClient _client;
        private readonly DiscordController _discord;
        private readonly IServiceProvider _services;
        public TicketController(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
            _discord = services.GetRequiredService<DiscordController>();
            _services = services;
        }

        public const string MongoConfigCollectionName = "ticketGuildConfig";
        protected static IMongoCollection<T>? GetConfigCollection<T>()
        {
            return Program.GetMongoDatabase()?.GetCollection<T>(MongoConfigCollectionName);
        }
        protected static IMongoCollection<ConfigGuildTicketModel>? GetConfigCollection()
            => GetConfigCollection<ConfigGuildTicketModel>();
        public async Task<ConfigGuildTicketModel?> GetGuildConfig(ulong guildId)
        {
            var collection = GetConfigCollection();
            var filter = Builders<ConfigGuildTicketModel>
                .Filter
                .Eq("GuildId", guildId);

            var item = await collection.FindAsync(filter);

            return item.FirstOrDefault();
        }

        public async Task SetGuildConfig(ConfigGuildTicketModel model)
        {
            var collection = GetConfigCollection();
            var filter = Builders<ConfigGuildTicketModel>
                .Filter
                .Eq("GuildId", model.GuildId);

            if ((await collection.FindAsync(filter)).Any())
                await collection.FindOneAndReplaceAsync(filter, model);
            else
                await collection.InsertOneAsync(model);
        }
        public async Task DeleteGuildConfig(ulong guildId)
        {
            var collection = GetConfigCollection();
            var filter = Builders<ConfigGuildTicketModel>
                .Filter
                .Eq("GuildId", guildId);

            await collection.DeleteManyAsync(filter);
        }
        public Task DeleteGuildConfig(ConfigGuildTicketModel model)
            => DeleteGuildConfig(model.GuildId);
    }
}
