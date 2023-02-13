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
    public class BanSyncController
    {
        private readonly DiscordSocketClient _client;
        private readonly DiscordController _discord;
        private readonly BanSyncConfigController _config;
        private readonly IServiceProvider _services;
        public BanSyncController(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
            _discord = services.GetRequiredService<DiscordController>();
            _config = services.GetRequiredService<BanSyncConfigController>();
            _services = services;
            _client.UserBanned += _client_UserBanned;
        }

        /// <summary>
        /// Add user to database
        /// </summary>
        private async Task _client_UserBanned(SocketUser user, SocketGuild guild)
        {
            if (await InfoExists(user.Id, guild.Id))
            {
                await RemoveInfo(user.Id, guild.Id);
            }

            var banInfo = await guild.GetBanAsync(user);

            var info = new BanSyncInfoModel()
            {
                UserId = user.Id,
                UserName = user.Username,
                UserDiscriminator = user.Discriminator.ToString(),
                GuildId = guild.Id,
                GuildName = guild.Name,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Reason = banInfo?.Reason ?? "null",
            };

            await NotifyBan(info);
        }

        /// <summary>
        /// TODO: Notify all guilds that the user is in that the user has been banned.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private async Task NotifyBan(BanSyncInfoModel info)
        {

        }
        }

        #region Mongo Helpers
        public const string MongoInfoCollectionName = "banSyncInfo";
        protected IMongoCollection<T>? GetInfoCollection<T>()
        {
            return Program.GetMongoDatabase()?.GetCollection<T>(MongoInfoCollectionName);
        }
        protected IMongoCollection<BanSyncInfoModel>? GetInfoCollection()
            => GetInfoCollection<BanSyncInfoModel>();
        protected async Task<IEnumerable<T>> BaseInfoFind<T>(FilterDefinition<T> data)
        {
            var collection = GetInfoCollection<T>();
            var result = await collection.FindAsync(data);

            return result.ToEnumerable();
        }
        protected async Task<bool> BaseInfoAny<T>(FilterDefinition<T> data)
        {
            var collection = GetInfoCollection<T>();
            var result = await collection.FindAsync(data);

            return result.Any();
        }
        protected async Task<T?> BaseInfoFirstOrDefault<T>(FilterDefinition<T> data)
        {
            var collection = GetInfoCollection<T>();
            var result = await collection.FindAsync(data);

            return result.FirstOrDefault();
        }
        #endregion

        #region Get/Set
        #region Get
        public async Task<IEnumerable<BanSyncInfoModel>> GetInfoEnumerable(ulong userId, ulong guildId)
        {
            var filter = Builders<BanSyncInfoModel>
                .Filter
                .Where(v => v.UserId == userId && v.GuildId == guildId);

            return await BaseInfoFind(filter);
        }
        public async Task<IEnumerable<BanSyncInfoModel>> GetInfoEnumerable(BanSyncInfoModel data)
            => await GetInfoEnumerable(data.UserId, data.GuildId);
        public async Task<IEnumerable<BanSyncInfoModel>> GetInfoEnumerable(ulong userId)
        {
            var filter = Builders<BanSyncInfoModel>
                .Filter
                .Eq("UserId", userId);

            return await BaseInfoFind(filter);
        }
        public async Task<BanSyncInfoModel?> GetInfo(ulong userId, ulong guildId)
        {
            var filter = Builders<BanSyncInfoModel>
                .Filter
                .Where(v => v.UserId == userId && v.GuildId == guildId);

            return await BaseInfoFirstOrDefault(filter);
        }
        public async Task<BanSyncInfoModel?> GetInfo(BanSyncInfoModel data)
            => await GetInfo(data.UserId, data.GuildId);
        #endregion

        public async Task SetInfo(BanSyncInfoModel data)
        {
            var collection = GetInfoCollection();
            var filter = Builders<BanSyncInfoModel>
                .Filter
                .Where(v => v.UserId == data.UserId && v.GuildId == data.GuildId);
            if (await InfoExists(data))
            {
                await collection.ReplaceOneAsync(filter, data);
            }
            else
            {
                await collection.InsertOneAsync(data);
            }
        }
        public async Task RemoveInfo(ulong userId, ulong guildId)
        {
            var collection = GetInfoCollection();
            var filter = Builders<BanSyncInfoModel>
                .Filter
                .Where(v => v.UserId == userId && v.GuildId == guildId);

            await collection.DeleteManyAsync(filter);
        }
        #endregion

        #region Info Exists
        public async Task<bool> InfoExists(ulong userId, ulong guildId)
        {
            var filter = Builders<BanSyncInfoModel>
                .Filter
                .Where(v => v.UserId == userId && v.GuildId == guildId);
            return await BaseInfoAny(filter);
        }
        public async Task<bool> InfoExists(ulong userId)
        {
            var filter = Builders<BanSyncInfoModel>
                .Filter
                .Eq("UserId", userId);
            return await BaseInfoAny(filter);
        }
        public async Task<bool> InfoExists(BanSyncInfoModel data)
            => await InfoExists(data.UserId, data.GuildId);
        #endregion
    }
}
