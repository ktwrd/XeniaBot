using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MongoDB.Driver;
using MongoDB.Driver.Core.Bindings;
using ShortcakeBot.Core.Models;
using ShortcakeBot.Core.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using IChannel = Discord.IChannel;

namespace ShortcakeBot.Core.Helpers
{
    public static class CounterHelper
    {
        /// <summary>
        /// Key: Channel Id
        /// Value: Value
        /// </summary>
        public static Dictionary<ulong, ulong> CachedItems = new Dictionary<ulong, ulong>();
        public static readonly string CollectionName = "countingGuildModel";
        public static IMongoCollection<T>? GetCollection<T>()
        {
            return Program.GetMongoDatabase()?.GetCollection<T>(CollectionName);
        }
        public static async Task Set(CounterGuildModel model)
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
        public static async Task<CounterGuildModel?> Get(IGuild guild)
        {
            var collection = GetCollection<CounterGuildModel>();

            var filter = Builders<CounterGuildModel>
                .Filter
                .Eq("GuildId", guild.Id);

            var result = await collection.FindAsync(filter);
            return result.FirstOrDefault();
        }
        public static async Task<CounterGuildModel?> Get<T>(IGuild guild, T channel) where T : IChannel
        {
            var collection = GetCollection<CounterGuildModel>();

            var filter = Builders<CounterGuildModel>
                .Filter
                .Eq("GuildId", guild.Id);
            
            var result = await collection.FindAsync(filter);
            var filtered = result.ToList().Where(v => v.ChannelId == channel.Id);
            return filtered.FirstOrDefault();
        }
        public static CounterGuildModel Get<T>(T channel) where T : IChannel
        {
            var collection = GetCollection<CounterGuildModel>();

            var filter = Builders<CounterGuildModel>
                .Filter
                .Eq("ChannelId", channel.Id);

            return collection.Find(filter).FirstOrDefault();
        }
        public static async Task<CounterGuildModel?> GetOrCreate(IGuild guild, IChannel channel)
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
        public static async Task<CounterGuildModel[]> GetAll()
        {
            var collection = GetCollection<CounterGuildModel>();
            var filter = Builders<CounterGuildModel>
                .Filter.Empty;
            var result = await collection.FindAsync(filter);
            return result.ToList().ToArray();
        }
        public static async Task<CounterGuildModel[]> GetAll(IChannel channel)
        {
            var collection = GetCollection<CounterGuildModel>();
            var filter = Builders<CounterGuildModel>
                .Filter
                .Eq("ChannelId", channel.Id);

            var result = await collection.FindAsync(filter);
            return result.ToList().ToArray();
        }
        public static async Task<CounterGuildModel[]> GetAll(IGuild guild)
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
        public static async Task Delete(CounterGuildModel model)
        {
            await Delete(model.ChannelId);
        }
        public static async Task Delete<T>(T channel) where T : IChannel
        {
            await Delete(channel.Id);
        }
        public static async Task Delete(ulong channelId)
        {
            var collection = GetCollection<CounterGuildModel>();
            var filter = Builders<CounterGuildModel>
                .Filter
                .Eq("ChannelId", channelId);
            await collection?.DeleteManyAsync(filter);
        }
        public static async Task Delete(IGuild guild)
        {
            var collection = GetCollection<CounterGuildModel>();
            var filter = Builders<CounterGuildModel>
            .Filter
                .Eq("GuildId", guild.Id);
            await collection?.DeleteManyAsync(filter);
        }
        #endregion

        #region Discord Events
        public static async Task Ready()
        {
            foreach (var item in await GetAll())
            {
                CachedItems.Add(item.ChannelId, item.Count);
            }
        }

        public static async Task MessageRecieved(SocketMessage arg)
        {
            if (!(arg is SocketUserMessage message))
                return;
            if (message.Source != MessageSource.User)
                return;
            if (!CachedItems.ContainsKey(arg.Channel.Id))
                return;

            ulong value = 0;
            try
            {
                value = ulong.Parse(arg.Content);
            }
            catch
            {
                return;
            }
            var context = new SocketCommandContext(Program.DiscordSocketClient, message);
            ulong targetValue = ulong.Parse(CachedItems[arg.Channel.Id].ToString()) + 1;
            if (value != targetValue)
            {
                await context.Guild.GetTextChannel(arg.Channel.Id).GetMessageAsync(arg.Id).Result.DeleteAsync();
                return;
            }


            var data = await Get(context.Guild);
            if (data == null)
            {
                data = new CounterGuildModel((IChannel)context.Channel, (IGuild)context.Guild);
            }

            data.Count = value;
            await Set(data);
        }
        #endregion

    }
}
