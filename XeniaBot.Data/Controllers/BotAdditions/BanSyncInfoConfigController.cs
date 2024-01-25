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
    private readonly DiscordSocketClient _discord;
    private readonly BanSyncStateHistoryConfigController _banSyncStateController;

    public BanSyncInfoConfigController(IServiceProvider services)
        : base("banSyncInfo", services)
    {
        _discord = services.GetRequiredService<DiscordSocketClient>();
        _banSyncStateController = services.GetRequiredService<BanSyncStateHistoryConfigController>();
    }
    protected async Task<ICollection<BanSyncInfoModel>> BaseInfoFind(FilterDefinition<BanSyncInfoModel> data)
    {
        var collection = GetCollection();
        var result = await collection.FindAsync(data);

        return result.ToList();
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
    public async Task<ICollection<BanSyncInfoModel>> GetInfoEnumerable(ulong userId, ulong guildId)
    {
        var filter = MongoDB.Driver.Builders<BanSyncInfoModel>
            .Filter
            .Where(v => v.UserId == userId && v.GuildId == guildId);

        return await BaseInfoFind(filter);
    }
    public async Task<ICollection<BanSyncInfoModel>> GetInfoEnumerable(BanSyncInfoModel data)
        => await GetInfoEnumerable(data.UserId, data.GuildId);
    public async Task<ICollection<BanSyncInfoModel>> GetInfoEnumerable(ulong userId)
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
        var res = await BaseInfoFind(filter);
        var sorted = res.OrderByDescending(v => v.Timestamp);
        return sorted.FirstOrDefault();
    }
    public async Task<BanSyncInfoModel?> GetInfo(BanSyncInfoModel data)
        => await GetInfo(data.UserId, data.GuildId);

    public async Task<List<BanSyncInfoModel>> GetInfoAllInGuild(ulong guildId, bool ignoreDisabledGuilds = true)
    {
        var result = new List<BanSyncInfoModel>();
        var guild = _discord.GetGuild(guildId);
        foreach (var user in guild.Users)
        {
            var filter = Builders<BanSyncInfoModel>
                .Filter
                .Where(v => v.UserId == user.Id);
            var userResult = await BaseInfoFind(filter);
            var innerUser = new List<BanSyncInfoModel>();
            foreach (var re in userResult)
            {
                var guildConf = await _banSyncStateController.GetLatest(re.GuildId);
                if (ignoreDisabledGuilds
                    && ((guildConf?.Enable ?? false)
                        || (guildConf?.State ?? BanSyncGuildState.Unknown) == BanSyncGuildState.Active))
                {
                    innerUser.Add(re);
                }
            }

            if (innerUser.Any())
            {
                result.Add(innerUser.OrderByDescending(v => v.Timestamp).FirstOrDefault());
            }
        }

        foreach (var i in await BaseInfoFind(
                     Builders<BanSyncInfoModel>
                         .Filter
                         .Where(v => v.GuildId == guildId)))
        {
            result.Add(i);
        }
        return result;
    }
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