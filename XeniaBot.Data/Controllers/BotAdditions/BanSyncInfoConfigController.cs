using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using XeniaBot.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using XeniaBot.Data.Controllers;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Models;

namespace XeniaBot.Data.Controllers.BotAdditions;

[BotController]
public class BanSyncInfoConfigController : BaseConfigController<BanSyncInfoModel>
{
    public BanSyncInfoConfigController(IServiceProvider services) : base("banSyncInfo", services)
    {}
    protected async Task<IEnumerable<BanSyncInfoModel>> BaseInfoFind(FilterDefinition<BanSyncInfoModel> data)
    {
        var collection = GetCollection();
        var result = await collection.FindAsync(data);

        return result.ToEnumerable();
    }
    protected async Task<bool> BaseInfoAny(FilterDefinition<BanSyncInfoModel> data)
    {
        var collection = GetCollection();
        var result = await collection.FindAsync(data);

        return result.Any();
    }
    protected async Task<BanSyncInfoModel?> BaseInfoFirstOrDefault(FilterDefinition<BanSyncInfoModel> data)
    {
        var collection = GetCollection();
        var result = await collection.FindAsync(data);

        return result.FirstOrDefault();
    }
        #region Get/Set
        #region Get
        public async Task<IEnumerable<BanSyncInfoModel>> GetInfoEnumerable(ulong userId, ulong guildId)
        {
            var filter = MongoDB.Driver.Builders<BanSyncInfoModel>
                .Filter
                .Where(v => v.UserId == userId && v.GuildId == guildId);

            return await BaseInfoFind(filter);
        }
        public async Task<IEnumerable<BanSyncInfoModel>> GetInfoEnumerable(BanSyncInfoModel data)
            => await GetInfoEnumerable(data.UserId, data.GuildId);
        public async Task<IEnumerable<BanSyncInfoModel>> GetInfoEnumerable(ulong userId)
        {
            var filter = MongoDB.Driver.Builders<BanSyncInfoModel>
                .Filter
                .Eq("UserId", userId);

            return await BaseInfoFind(filter);
        }
        public async Task<BanSyncInfoModel?> GetInfo(ulong userId, ulong guildId)
        {
            var filter = MongoDB.Driver.Builders<BanSyncInfoModel>
                .Filter
                .Where(v => v.UserId == userId && v.GuildId == guildId);

            return await BaseInfoFirstOrDefault(filter);
        }
        public async Task<BanSyncInfoModel?> GetInfo(BanSyncInfoModel data)
            => await GetInfo(data.UserId, data.GuildId);
        #endregion

        public async Task SetInfo(BanSyncInfoModel data)
        {
            var collection = GetCollection();
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
            var collection = GetCollection();
            var filter = MongoDB.Driver.Builders<BanSyncInfoModel>
                .Filter
                .Where(v => v.UserId == userId && v.GuildId == guildId);

            await collection.DeleteManyAsync(filter);
        }
        #endregion

        #region Info Exists
        public async Task<bool> InfoExists(ulong userId, ulong guildId)
        {
            var filter = MongoDB.Driver.Builders<BanSyncInfoModel>
                .Filter
                .Where(v => v.UserId == userId && v.GuildId == guildId);
            return await BaseInfoAny(filter);
        }
        public async Task<bool> InfoExists(ulong userId)
        {
            var filter = MongoDB.Driver.Builders<BanSyncInfoModel>
                .Filter
                .Eq("UserId", userId);
            return await BaseInfoAny(filter);
        }
        public async Task<bool> InfoExists(BanSyncInfoModel data)
            => await InfoExists(data.UserId, data.GuildId);
        #endregion
}