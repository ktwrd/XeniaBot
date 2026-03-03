using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace XeniaBot.Shared.Repositories;

public interface IBanSyncInfoRepository<TRecord>
{
    public Task<ICollection<TRecord>> GetAll();
    public Task<long> CountInGuild(ulong guildId, bool allowGhost = false);
    public Task<ICollection<TRecord>> GetInfoEnumerable(TRecord data, bool allowGhost = false);
    public Task<ICollection<TRecord>> GetInfoEnumerable(ulong userId, bool allowGhost = false);
    public Task<ICollection<TRecord>> GetInfoEnumerable(ulong userId, ulong guildId, bool allowGhost = false);
    public Task<TRecord?> GetInfo(ulong userId, ulong guildId, bool allowGhost = false);
    public Task<TRecord?> GetInfo(TRecord data, bool allowGhost = false);
    public Task<TRecord?> GetInfo(Guid id, bool allowGhost = false);
    public Task<ICollection<TRecord>> GetInfoAllInGuild(ulong guildId, bool ignoreDisabledGuilds = false, bool allowGhost = false);
    public Task<ICollection<TRecord>> GetInfoAllInGuildPaginate(
        ulong guildId,
        int page,
        int pageSize,
        ulong? filterByUserId,
        bool ignoreDisabledGuilds = false,
        bool allowGhost = false);
    public Task<long> GetInfoAllInGuildCount(
        ulong guildId,
        ulong? filterByUserId,
        bool ignoreDisabledGuilds = false,
        bool allowGhost = false);
    public Task SetInfo(TRecord data);
    public Task RemoveInfo(TRecord data);
    public Task<bool> InfoExists(ulong userId, ulong guildId);
    public Task<bool> InfoExists(ulong userId);
}
