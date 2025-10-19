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
public class RoleConfigRepository : BaseRepository<RoleConfigModel>
{
    public RoleConfigRepository(IServiceProvider services)
        : base("roleConfig", services)
    {
    }

    #region Get
    public async Task<RoleConfigModel?> Get(ulong guildId, string name)
    {
        var filter = Builders<RoleConfigModel>
            .Filter
            .Where(v => v.GuildId == guildId && v.Name == name);

        var results = await BaseFind(filter, limit: 1);
        return results.FirstOrDefault();
    }
    public async Task<RoleConfigModel?> Get(ulong guildId, ulong roleId)
    {
        var filter = Builders<RoleConfigModel>
            .Filter
            .Where(v => v.GuildId == guildId && v.RoleId == roleId);

        var results = await BaseFind(filter, limit: 1);
        return results.FirstOrDefault();
    }
    #endregion

    #region Get All
    /// <param name="all">When true, all other filter parameters are ignored</param>
    /// <param name="guildId"></param>
    /// <param name="roleId"></param>
    /// <param name="requiredRoleId"></param>
    /// <param name="blacklistRoleId"></param>
    /// <param name="name"></param>
    /// <param name="group"></param>
    /// <returns>IEnumerable of role configs</returns>
    public async Task<ICollection<RoleConfigModel>?> GetAll(
        bool all = false,
        ulong? guildId = null,
        ulong? roleId = null,
        string? uid = null,
        ulong? requiredRoleId = null,
        ulong? blacklistRoleId = null,
        string? name = null,
        string? group = null)
    {
        FilterDefinition<RoleConfigModel>? filter = Builders<RoleConfigModel>.Filter.Empty;
        
        if (!all)
        {
            Func<RoleConfigModel, bool> matchLogic = (v) =>
            {
                int c = 0;
                int mc = 0;

                // set mc based on the parameters
                mc += guildId == null ? 0 : 1;
                mc += roleId == null ? 0 : 1;
                mc += uid == null ? 0 : 1;
                mc += requiredRoleId == null ? 0 : 1;
                mc += blacklistRoleId == null ? 0 : 1;
                mc += name == null ? 0 : 1;
                mc += group == null ? 0 : 1;

                c += v.GuildId == guildId ? 1 : 0;
                c += v.RoleId == roleId ? 1 : 0;
                c += v.Uid == uid ? 1 : 0;
                c += v.RequiredRoleId == requiredRoleId ? 1 : 0;
                c += v.BlacklistRoleId == blacklistRoleId ? 1 : 0;
                c += v.Name == name ? 1 : 0;
                c += v.Group == group ? 1 : 0;

                return c >= mc;
            };
            filter = Builders<RoleConfigModel>
                .Filter
                .Where((v) => matchLogic(v));
        }


        var results = await BaseFind(filter);
        return results.ToList();
    }

    public async Task<ICollection<RoleConfigModel>?> GetAll()
        => await GetAll(true);
    public async Task<ICollection<RoleConfigModel>?> GetAll(RoleConfigModel model)
        => await GetAll(
            false,
            model.GuildId,
            model.RoleId,
            model.Uid,
            model.RequiredRoleId,
            model.BlacklistRoleId,
            model.Name,
            model.Group);
    #endregion

    #region Set
    public async Task Set(RoleConfigModel model)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var filter = Builders<RoleConfigModel>
            .Filter
            .Where(e => e.Uid == model.Uid);

        var exists = (await collection.CountDocumentsAsync(filter)) > 0;
        if (exists)
        {
            await collection.ReplaceOneAsync(filter, model);
        }
        else
        {
            await collection.InsertOneAsync(model);
        }
    }
    #endregion

}
