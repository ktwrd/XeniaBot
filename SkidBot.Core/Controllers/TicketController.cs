using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using SkidBot.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
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

        #region MongoDB TicketModel Boilerplate
        public const string MongoTranscriptCollectionName = "ticketTranscript";
        protected static IMongoCollection<T>? GetTranscriptCollection<T>()
        {
            return Program.GetMongoDatabase()?.GetCollection<T>(MongoTicketCollectionName);
        }
        protected static IMongoCollection<TicketTranscriptModel>? GetTranscriptCollection()
            => GetTranscriptCollection<TicketTranscriptModel>();
        public async Task<TicketTranscriptModel?> GetTranscript(string transcriptUid)
        {
            var collection = GetTranscriptCollection();
            var filter = Builders<TicketTranscriptModel>
                .Filter
                .Eq("Uid", transcriptUid);

            var items = await collection.FindAsync(filter);

            return items.FirstOrDefault();
        }
        public async Task<TicketTranscriptModel?> GenerateTranscript(TicketModel ticket)
        {
            var guild = _client.GetGuild(ticket.GuildId);
            if (guild == null)
                throw new Exception($"Failed to fetch guild {ticket.GuildId}");
            var channel = guild.GetTextChannel(ticket.ChannelId);
            if (channel == null)
                throw new Exception($"Failed to fetch ticket channel {ticket.ChannelId}");

            // Fetch all messages in channel
            var messages = await channel.GetMessagesAsync(int.MaxValue).FlattenAsync();

            var model = new TicketTranscriptModel()
            {
                TicketUid = ticket.Uid,
            };
            var _tk = await Get(ticket.ChannelId);
            if (_tk != null)
            {
                _tk.TranscriptUid = model.Uid;
                await Set(_tk);
            }

            model.Messages = messages.ToArray();

            var collection = GetTranscriptCollection();
            await collection.InsertOneAsync(model);

            return model;
        }

        public const string MongoTicketCollectionName = "ticketDetails";
        protected static IMongoCollection<T>? GetTicketCollection<T>()
        {
            return Program.GetMongoDatabase()?.GetCollection<T>(MongoTicketCollectionName);
        }
        protected static IMongoCollection<TicketModel>? GetTicketCollection()
            => GetConfigCollection<TicketModel>();

        public async Task<TicketModel?> Get(ulong channelId)
        {
            var collection = GetTicketCollection();
            var filter = Builders<TicketModel>
                .Filter
                .Eq("ChannelId", channelId);

            var items = await collection.FindAsync(filter);

            return items.FirstOrDefault();
        }
        public async Task Set(TicketModel model)
        {
            var collection = GetTicketCollection();
            var filter = Builders<TicketModel>
                .Filter
                .Eq("ChannelId", model.ChannelId);

            if ((await collection.FindAsync(filter)).Any())
                await collection.FindOneAndReplaceAsync(filter, model);
            else
                await collection.InsertOneAsync(model);
        }
        #endregion

        #region MongoDB Config Boilerplate
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
        #endregion
    }
}
