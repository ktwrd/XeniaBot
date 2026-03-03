using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using XeniaBot.Data.Models;
using XeniaBot.Shared;
using XeniaBot.Shared.Repositories;

namespace XeniaBot.Data.Repositories;

[XeniaController]
public class BanSyncInfoRepository
    : BaseRepository<BanSyncInfoModel>
    , IBanSyncInfoRepository<BanSyncInfoModel>
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly DiscordSocketClient _discord;
    private readonly BanSyncStateHistoryRepository _banSyncStateController;

    public BanSyncInfoRepository(IServiceProvider services)
        : base(BanSyncInfoModel.CollectionName, services)
    {
        _discord = services.GetRequiredService<DiscordSocketClient>();
        _banSyncStateController = services.GetRequiredService<BanSyncStateHistoryRepository>();
        var collectionName = BanSyncInfoModel.CollectionName;

        var collection = GetCollection();
        if (collection == null)
        {
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        }

        var existingIndexes = collection.Indexes.List().ToList();
        var targetIndexes = new Dictionary<string, IndexKeysDefinition<BanSyncInfoModel>>()
        {
            {
                collectionName + "_IX_UserIdGuildId",
                Builders<BanSyncInfoModel>
                    .IndexKeys
                    .Descending(e => e.UserId)
                    .Descending(e => e.GuildId)
            },
            {
                collectionName + "_IX_UserId",
                Builders<BanSyncInfoModel>
                    .IndexKeys
                    .Descending(e => e.UserId)
            },
            {
                collectionName + "_IX_GuildId",
                Builders<BanSyncInfoModel>
                    .IndexKeys
                    .Descending(e => e.GuildId)
            },
            {
                collectionName + "_IX_Timestamp",
                Builders<BanSyncInfoModel>
                    .IndexKeys
                    .Descending(e => e.Timestamp)
            }
        };
        foreach (var (name, idx) in targetIndexes)
        {
            if (!existingIndexes.Any(e => e.GetElement("name").Value.AsString == name))
            {
                var model = new CreateIndexModel<BanSyncInfoModel>(idx, new CreateIndexOptions()
                {
                    Name = name
                });
                try
                {
                    collection.Indexes.CreateOne(model);
                    _log.Info($"{collectionName} - Created index \"{name}\"");
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"{collectionName} - Failed to create index \"{name}\"");
                }
            }
        }
    }
    
    private static readonly SortDefinition<BanSyncInfoModel> sort_timestamp
        = Builders<BanSyncInfoModel>
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
        return await res.ToListAsync();
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
            .Where(v => v.GuildId == guildId);

        if (!allowGhost)
        {
            filter &= Builders<BanSyncInfoModel>
                .Filter
                .Where(v => !v.Ghost);
        }

        return await collection.CountDocumentsAsync(filter);
    }
    public async Task<ICollection<BanSyncInfoModel>> GetInfoEnumerable(BanSyncInfoModel data, bool allowGhost = false)
        => await GetInfoEnumerable(data.UserId, data.GuildId, allowGhost);
    public async Task<ICollection<BanSyncInfoModel>> GetInfoEnumerable(ulong userId, bool allowGhost = false)
    {
        var filter = Builders<BanSyncInfoModel>
            .Filter
            .Where(v => v.UserId == userId);

        if (!allowGhost)
        {
            filter &= Builders<BanSyncInfoModel>
                .Filter
                .Where(v => !v.Ghost);
        }

        var res = await BaseFind(filter);
        return await res.ToListAsync();
    }
    public async Task<ICollection<BanSyncInfoModel>> GetInfoEnumerable(ulong userId, ulong guildId, bool allowGhost = false)
    {
        var filter = Builders<BanSyncInfoModel>
            .Filter
            .Where(v => v.UserId == userId && v.GuildId == guildId);

        if (!allowGhost)
        {
            filter &= Builders<BanSyncInfoModel>
                .Filter
                .Where(v => !v.Ghost);
        }

        var res = await BaseFind(filter);
        return await res.ToListAsync();
    }
    public async Task<BanSyncInfoModel?> GetInfo(ulong userId, ulong guildId, bool allowGhost = false)
    {
        var filter = Builders<BanSyncInfoModel>
            .Filter
            .Where(v => v.UserId == userId && v.GuildId == guildId);
        if (!allowGhost)
        {
            filter &= Builders<BanSyncInfoModel>
                .Filter
                .Where(v => !v.Ghost);
        }
        var res = await BaseFind(filter, sort_timestamp, limit: 1);
        return await res.FirstOrDefaultAsync();
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
        if (!allowGhost)
        {
            filter &= Builders<BanSyncInfoModel>.Filter.Where(v => !v.Ghost);
        }

        var res = await BaseFind(filter, sort_timestamp, limit: 1);
        return await res.FirstOrDefaultAsync();
    }
    public Task<BanSyncInfoModel?> GetInfo(Guid id, bool allowGhost = false)
        => GetInfo(id.ToString(), allowGhost);
    
    public async Task<ICollection<BanSyncInfoModel>> GetInfoAllInGuild(ulong guildId, bool ignoreDisabledGuilds = false, bool allowGhost = false)
    {
        var result = new List<BanSyncInfoModel>();
        var guild = _discord.GetGuild(guildId);
        foreach (var user in guild.Users)
        {
            var filter = Builders<BanSyncInfoModel>
                .Filter
                .Where(v => v.UserId == user.Id);
            if (!allowGhost)
            {
                filter &= Builders<BanSyncInfoModel>
                    .Filter
                    .Where(v => !v.Ghost);
            }
            var userResult = await BaseFind(filter);
            var innerUser = new List<BanSyncInfoModel>();
            foreach (var re in await userResult.ToListAsync())
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
        foreach (var i in await currentServerResult.ToListAsync())
        {
            result.Add(i);
        }
        return result;
    }

    private FilterDefinition<BanSyncInfoModel> GetInfoAllInGuild_Filter(
        ulong guildId,
        ulong? filterByUserId,
        bool ignoreDisabledGuilds = false,
        bool allowGhost = false,
        ulong[]? includeUsers = null)
    {
        var filter = Builders<BanSyncInfoModel>
            .Filter
            .Where(e => e.GuildId == guildId);
        if (filterByUserId != null)
        {
            filter = Builders<BanSyncInfoModel>
                .Filter
                .Where(v => v.UserId == filterByUserId);
        }
        if (includeUsers != null)
        {
            filter |= Builders<BanSyncInfoModel>
                .Filter
                .In(v => v.UserId, includeUsers);
        }
        if (!allowGhost)
        {
            filter &= Builders<BanSyncInfoModel>
                .Filter
                .Where(v => !v.Ghost);
        }

        if (ignoreDisabledGuilds)
        {
            throw new NotImplementedException($"Logic for parameter {nameof(ignoreDisabledGuilds)} is not implemented");
        }

        filter |= Builders<BanSyncInfoModel>
            .Filter
            .Where(v => v.GuildId == guildId);

        return filter;
    }
    public async Task<ICollection<BanSyncInfoModel>> GetInfoAllInGuildPaginate(ulong guildId,
        int page,
        int pageSize,
        ulong? filterByUserId,
        bool ignoreDisabledGuilds = false,
        bool allowGhost = false)
    {
        var guild = _discord.GetGuild(guildId);
        var userIdList = guild.Users.Select(v => v.Id).ToArray();
        var filter = GetInfoAllInGuild_Filter(guildId, filterByUserId, ignoreDisabledGuilds, allowGhost, userIdList);

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
        var guild = _discord.GetGuild(guildId);
        var userIdList = guild.Users.Select(v => v.Id).ToArray();
        var filter = GetInfoAllInGuild_Filter(guildId, filterByUserId, ignoreDisabledGuilds, allowGhost, userIdList);

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
    public Task RemoveInfo(Guid recordId) => RemoveInfo(recordId.ToString());
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