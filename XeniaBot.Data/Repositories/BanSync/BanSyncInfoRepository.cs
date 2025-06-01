using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using XeniaBot.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XeniaBot.Data.Models;
using System.Data;

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
            .Where(v => v.UserId == userId && v.GuildId == guildId);
        
        if (!allowGhost)
        {
            filter &= Builders<BanSyncInfoModel>
                .Filter
                .Where(v => !v.Ghost);
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
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");

        var filter = Builders<BanSyncInfoModel>
            .Filter
            .Where(v => v.GuildId == guildId && (!allowGhost && !v.Ghost));
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
            filter = Builders<BanSyncInfoModel>
                .Filter
                .Where(e => e.UserId == userId);
        }

        var res = await BaseFind(filter);
        return res.ToList();
    }
    public async Task<BanSyncInfoModel?> GetInfo(ulong userId, ulong guildId, bool allowGhost = false)
    {
        var filter = Builders<BanSyncInfoModel>
            .Filter
            .Where(v => v.UserId == userId && v.GuildId == guildId && !v.Ghost);
        if (allowGhost)
        {
            filter = Builders<BanSyncInfoModel>
                .Filter
                .Where(v => v.UserId == userId && v.GuildId == guildId);
        }
        var res = await BaseFind(filter, sort_timestamp, limit: 1);
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

        var res = await BaseFind(filter, sort_timestamp, limit: 1);
        return res.FirstOrDefault();
    }
    
    public async Task<List<BanSyncInfoModel>> GetInfoAllInGuild(ulong guildId, bool ignoreDisabledGuilds = false, bool allowGhost = false)
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
                if (ignoreDisabledGuilds)
                {
                    if (guildConf?.Enable == true &&
                        (guildConf?.State ?? BanSyncGuildState.Unknown) == BanSyncGuildState.Active)
                    {
                        innerUser.Add(re);
                    }
                }
                else
                {
                    innerUser.Add(re);
                }
            }

            var targetUser = innerUser.OrderByDescending(e => e.Timestamp).FirstOrDefault();
            if (targetUser != null)
            {
                result.Add(targetUser);
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

    private FilterDefinition<BanSyncInfoModel> GetInfoAllInGuild_Filter(
        ulong guildId,
        ulong? filterByUserId,
        bool ignoreDisabledGuilds = false,
        bool allowGhost = false)
    {
        
        var guild = _discord.GetGuild(guildId);
        var userIdList = guild.Users.Select(v => v.Id);
        var filter = Builders<BanSyncInfoModel>
            .Filter
            .In(e => e.UserId, userIdList);
        if (filterByUserId != null)
        {
            filter = Builders<BanSyncInfoModel>
                .Filter
                .Where(v => v.UserId == filterByUserId);
        }
        if (allowGhost == false)
        {
            filter &= Builders<BanSyncInfoModel>
                .Filter
                .Where(v => v.Ghost == false);
        }

        filter |= Builders<BanSyncInfoModel>
            .Filter
            .Where(e => e.GuildId == guildId);

        if (ignoreDisabledGuilds)
        {
            throw new NotImplementedException($"Logic for parameter {nameof(ignoreDisabledGuilds)} is not implemented");
        }

        filter |= Builders<BanSyncInfoModel>
            .Filter
            .Where(v => v.GuildId == guildId);

        return filter;
    }
    public async Task<List<BanSyncInfoModel>> GetInfoAllInGuildPaginate(ulong guildId,
        int page,
        int pageSize,
        ulong? filterByUserId,
        bool ignoreDisabledGuilds = false,
        bool allowGhost = false)
    {
        var filter = GetInfoAllInGuild_Filter(guildId, filterByUserId, ignoreDisabledGuilds, allowGhost);

        var collection = GetCollection();
        
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");

        var result = await collection.Find(filter)
            .SortByDescending(v => v.Timestamp)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
        return result;
    }

    public async Task<long> GetInfoAllInGuildCount(ulong guildId,
        ulong? filterByUserId,
        bool ignoreDisabledGuilds = false,
        bool allowGhost = false)
    {
        var filter = GetInfoAllInGuild_Filter(guildId, filterByUserId, ignoreDisabledGuilds, allowGhost);

        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");

        var count = await collection.CountDocumentsAsync(filter);
        return count;
    }
    #endregion

    public async Task SetInfo(BanSyncInfoModel data)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
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
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var filter = Builders<BanSyncInfoModel>
            .Filter
            .Where(v => v.RecordId == recordId);

        await collection.DeleteManyAsync(filter);
    }
    #endregion

    #region Info Exists
    public async Task<bool> InfoExists(ulong userId, ulong guildId)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var filter = Builders<BanSyncInfoModel>
            .Filter
            .Where(v => v.UserId == userId && v.GuildId == guildId);
        return await collection.CountDocumentsAsync(filter) > 0;
    }
    public async Task<bool> InfoExists(ulong userId)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var filter = Builders<BanSyncInfoModel>
            .Filter
            .Where(e => e.UserId == userId);
        return await collection.CountDocumentsAsync(filter) > 0;
    }
    public async Task<bool> InfoExists(BanSyncInfoModel data)
        => await InfoExists(data.UserId, data.GuildId);
    #endregion
}