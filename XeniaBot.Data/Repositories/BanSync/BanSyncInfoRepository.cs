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
using XeniaBot.Data.Models;

namespace XeniaBot.Data.Repositories;

[XeniaController]
public class BanSyncInfoRepository : BaseRepository<BanSyncInfoModel>
{
    private readonly DiscordSocketClient _discord;
    private readonly BanSyncStateHistoryRepository _banSyncStateController;

    public BanSyncInfoRepository(IServiceProvider services)
        : base(BanSyncInfoModel.CollectionName, services)
    {
        _discord = services.GetRequiredService<DiscordSocketClient>();
        _banSyncStateController = services.GetRequiredService<BanSyncStateHistoryRepository>();
    }
    
    private SortDefinition<BanSyncInfoModel> sort_timestamp
        => Builders<BanSyncInfoModel>
            .Sort
            .Descending(v => v.Timestamp);

    #region Get/Set
    #region Get
    public async Task<ICollection<BanSyncInfoModel>> GetAll()
    {
        var filter = Builders<BanSyncInfoModel>
            .Filter
            .Empty;
        var res = await BaseFind(filter);
        return res.ToList();
    }
    public async Task<ICollection<BanSyncInfoModel>> GetInfoEnumerable(ulong userId, ulong guildId, bool allowGhost = false)
    {
        var filter = MongoDB.Driver.Builders<BanSyncInfoModel>
            .Filter
            .Where(v => v.UserId == userId && v.GuildId == guildId && !v.Ghost);
        
        if (allowGhost)
        {
            filter = MongoDB.Driver.Builders<BanSyncInfoModel>
                .Filter
                .Where(v => v.UserId == userId && v.GuildId == guildId);
        }

        var res = await BaseFind(filter);
        return res.ToList();
    }

    /// <summary>
    /// Fetch amount of BanSync records that exist for a Guild.
    /// </summary>
    /// <param name="guildId"><see cref="BanSyncInfoModel.GuildId"/></param>
    /// <param name="allowGhost">Include ghosted records.</param>
    /// <returns>Amount of records.</returns>
    public async Task<long> CountInGuild(ulong guildId, bool allowGhost = false)
    {
        var filter = Builders<BanSyncInfoModel>
            .Filter
            .Where(v => v.GuildId == guildId && (!allowGhost && !v.Ghost));
        var collection = GetCollection();
        return await collection.CountDocumentsAsync(filter);
    }
    public async Task<ICollection<BanSyncInfoModel>> GetInfoEnumerable(BanSyncInfoModel data, bool allowGhost = false)
        => await GetInfoEnumerable(data.UserId, data.GuildId, allowGhost);
    public async Task<ICollection<BanSyncInfoModel>> GetInfoEnumerable(ulong userId, bool allowGhost = false)
    {
        var filter = Builders<BanSyncInfoModel>
            .Filter
            .Where(v => v.UserId == userId && !v.Ghost);
        if (allowGhost)
        {
            filter = MongoDB.Driver.Builders<BanSyncInfoModel>
                .Filter
                .Eq("UserId", userId);
        }

        var res = await BaseFind(filter);
        return res.ToList();
    }
    public async Task<BanSyncInfoModel?> GetInfo(ulong userId, ulong guildId, bool allowGhost = false)
    {
        var filter = MongoDB.Driver.Builders<BanSyncInfoModel>
            .Filter
            .Where(v => v.UserId == userId && v.GuildId == guildId && !v.Ghost);
        if (allowGhost)
        {
            filter = MongoDB.Driver.Builders<BanSyncInfoModel>
                .Filter
                .Where(v => v.UserId == userId && v.GuildId == guildId);
        }
        var res = await BaseFind(filter, sort_timestamp);
        return res.FirstOrDefault();
    }
    public async Task<BanSyncInfoModel?> GetInfo(BanSyncInfoModel data, bool allowGhost = false)
        => await GetInfo(data.RecordId, allowGhost);

    /// <summary>
    /// Fetch a document by <see cref="BanSyncInfoModel.RecordId"/>
    /// </summary>
    /// <param name="id"><see cref="BanSyncInfoModel.RecordId"/></param>
    /// <param name="allowGhost">When `true`, the result will include records that have <see cref="BanSyncInfoModel.Ghost"/> set to `true`</param>
    /// <returns>`null` when not found or ghosted.</returns>
    public async Task<BanSyncInfoModel?> GetInfo(string id, bool allowGhost = false)
    {
        var filter = Builders<BanSyncInfoModel>
            .Filter
            .Where(v => v.RecordId == id);
        if (allowGhost == false)
        {
            filter &= Builders<BanSyncInfoModel>.Filter.Where(v => v.Ghost == false);
        }

        var res = await BaseFind(filter, sort_timestamp);
        return res.FirstOrDefault();
    }
    
    public async Task<List<BanSyncInfoModel>> GetInfoAllInGuild(ulong guildId, bool ignoreDisabledGuilds = true, bool allowGhost = false)
    {
        var result = new List<BanSyncInfoModel>();
        var guild = _discord.GetGuild(guildId);
        foreach (var user in guild.Users)
        {
            var filter = Builders<BanSyncInfoModel>
                .Filter
                .Where(v => v.UserId == user.Id && !v.Ghost);
            if (allowGhost)
            {
                filter = Builders<BanSyncInfoModel>
                    .Filter
                    .Where(v => v.UserId == user.Id);
            }
            var userResult = await BaseFind(filter);
            var innerUser = new List<BanSyncInfoModel>();
            foreach (var re in userResult.ToList())
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

        var currentServerFilter = Builders<BanSyncInfoModel>
            .Filter
            .Where(v => v.GuildId == guildId);
        var currentServerResult = await BaseFind(currentServerFilter);
        foreach (var i in currentServerResult.ToList())
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
            .Where(v => v.RecordId == data.RecordId);
        if (await InfoExists(data))
        {
            await collection.DeleteManyAsync(filter);
        }
        await collection.InsertOneAsync(data);
    }
    public async Task RemoveInfo(string recordId)
    {
        var collection = GetCollection();
        var filter = MongoDB.Driver.Builders<BanSyncInfoModel>
            .Filter
            .Where(v => v.RecordId == recordId);

        await collection.DeleteManyAsync(filter);
    }
    #endregion

    #region Info Exists
    public async Task<bool> InfoExists(ulong userId, ulong guildId)
    {
        var filter = MongoDB.Driver.Builders<BanSyncInfoModel>
            .Filter
            .Where(v => v.UserId == userId && v.GuildId == guildId);
        var res = await BaseFind(filter);
        return await res.AnyAsync();
    }
    public async Task<bool> InfoExists(ulong userId)
    {
        var filter = MongoDB.Driver.Builders<BanSyncInfoModel>
            .Filter
            .Eq("UserId", userId);
        var res = await BaseFind(filter);
        return await res.AnyAsync();
    }
    public async Task<bool> InfoExists(BanSyncInfoModel data)
        => await InfoExists(data.UserId, data.GuildId);
    #endregion
}