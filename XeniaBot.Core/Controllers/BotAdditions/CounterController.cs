using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using XeniaBot.Core.Helpers;
using XeniaBot.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Data.Controllers;
using XeniaBot.Data.Models;

namespace XeniaBot.Core.Controllers.BotAdditions
{
    [BotController]
    public class CounterController : BaseController
    {
        protected static Dictionary<ulong, ulong> CachedItems = new Dictionary<ulong, ulong>();

        private readonly DiscordSocketClient _client;
        private readonly DiscordController _discord;
        public CounterController(IServiceProvider services)
            : base(services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
            _discord = services.GetRequiredService<DiscordController>();
        }
        public override Task InitializeAsync()
        {
            _discord.MessageReceived += DiscordMessageReceived;

            return Task.CompletedTask;
        }
        public override async Task OnReady()
        {
            foreach (var item in await GetAll())
            {
                CachedItems.Add(item.ChannelId, item.Count);
            }
        }
        #region MongoDB Wrapper
        public const string MongoCollectionName = "countingGuildModel";
        protected static IMongoCollection<T>? GetCollection<T>()
        {
            return Program.GetMongoDatabase()?.GetCollection<T>(MongoCollectionName);
        }
        protected static IMongoCollection<CounterGuildModel>? GetCollection()
            => GetCollection<CounterGuildModel>();
        public async Task Set(CounterGuildModel model)
        {
            var collection = GetCollection<CounterGuildModel>();

            var filter = Builders<CounterGuildModel>
                .Filter
                .Eq("GuildId", model.GuildId);

            if (collection?.Find(filter).Any() ?? false)
            {
                await collection?.ReplaceOneAsync(filter, model);
            }
            else
            {
                await collection?.InsertOneAsync(model);
            }
            if (!CachedItems.ContainsKey(model.ChannelId))
                CachedItems.Add(model.ChannelId, model.Count);
            CachedItems[model.ChannelId] = model.Count;
        }
        #region Get
        /// <returns><see cref="null"/> when doesn't exist</returns>
        public async Task<CounterGuildModel?> Get(IGuild guild)
        {
            var collection = GetCollection<CounterGuildModel>();

            var filter = Builders<CounterGuildModel>
                .Filter
                .Eq("GuildId", guild.Id);

            var result = await collection.FindAsync(filter);
            return result.FirstOrDefault();
        }
        public async Task<CounterGuildModel> Get<T>(IGuild guild, T channel) where T : IChannel
        {
            var collection = GetCollection<CounterGuildModel>();

            var filter = Builders<CounterGuildModel>
                .Filter
                .Eq("GuildId", guild.Id);

            var result = await collection.FindAsync(filter);
            var filtered = result.ToList().Where(v => v.ChannelId == channel.Id);
            return filtered.FirstOrDefault() ?? new CounterGuildModel(channel, guild);
        }
        public CounterGuildModel Get<T>(T channel) where T : IChannel
        {
            var collection = GetCollection<CounterGuildModel>();

            var filter = Builders<CounterGuildModel>
                .Filter
                .Eq("ChannelId", channel.Id);

            return collection.Find(filter).FirstOrDefault();
        }
        public async Task<CounterGuildModel?> GetOrCreate(IGuild guild, IChannel channel)
        {
            CounterGuildModel? data = await Get(guild, channel);
            if (data != null)
                return data;

            data = new CounterGuildModel(channel, guild);
            await Set(data);
            return data;
        }
        #endregion
        #region Get All
        public async Task<CounterGuildModel[]> GetAll()
        {
            var collection = GetCollection<CounterGuildModel>();
            var filter = Builders<CounterGuildModel>
                .Filter.Empty;
            var result = await collection.FindAsync(filter);
            return result.ToList().ToArray();
        }
        public async Task<CounterGuildModel[]> GetAll(IChannel channel)
        {
            var collection = GetCollection<CounterGuildModel>();
            var filter = Builders<CounterGuildModel>
                .Filter
                .Eq("ChannelId", channel.Id);

            var result = await collection.FindAsync(filter);
            return result.ToList().ToArray();
        }
        public async Task<CounterGuildModel[]> GetAll(IGuild guild)
        {
            var collection = GetCollection<CounterGuildModel>();
            var filter = Builders<CounterGuildModel>
            .Filter
                .Eq("GuildId", guild.Id);

            var result = await collection.FindAsync(filter);
            return result.ToList().ToArray();
        }
        #endregion
        #region Delete
        public async Task Delete(CounterGuildModel model)
        {
            await Delete(model.ChannelId);
        }
        public async Task Delete<T>(T channel) where T : IChannel
        {
            await Delete(channel.Id);
        }
        public async Task Delete(ulong channelId)
        {
            var collection = GetCollection<CounterGuildModel>();
            var filter = Builders<CounterGuildModel>
                .Filter
                .Eq("ChannelId", channelId);
            await collection?.DeleteManyAsync(filter);
        }
        public async Task Delete(IGuild guild)
        {
            var collection = GetCollection<CounterGuildModel>();
            var filter = Builders<CounterGuildModel>
            .Filter
                .Eq("GuildId", guild.Id);
            await collection?.DeleteManyAsync(filter);
        }
        #endregion
        #endregion
        private async Task DiscordMessageReceived(SocketMessage arg)
        {
            if (!(arg is SocketUserMessage message))
                return;
            if (message.Source != MessageSource.User)
                return;
            if (!CachedItems.ContainsKey(arg.Channel.Id))
                return;

            // Try and parse the content as a ulong, if we fail
            // then we delete the message. FormatException is
            // thrown when we fail to parse as a ulong.
            ulong value = 0;
            try
            {
                value = ulong.Parse(arg.Content);
            }
            catch (FormatException)
            {
                await DiscordHelper.DeleteMessage(_client, arg);
                return;
            }

            // If number is not the next number, then we delete the message.
            var context = new SocketCommandContext(_client, message);
            ulong targetValue = value + 1;
            if (value != targetValue)
            {
                await DiscordHelper.DeleteMessage(_client, arg);
                return;
            }

            // Update record
            CounterGuildModel data = await Get(context.Guild, context.Channel);
            data.Count = value;
            await Set(data);
        }
    }
}
