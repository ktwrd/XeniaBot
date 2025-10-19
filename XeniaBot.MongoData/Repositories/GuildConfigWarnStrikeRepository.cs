using System;
using System.Data;
using System.Threading.Tasks;
using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Repositories;

[XeniaController]
public class GuildConfigWarnStrikeRepository : BaseRepository<GuildConfigWarnStrikeModel>
{
    public GuildConfigWarnStrikeRepository(IServiceProvider services)
        : base(GuildConfigWarnStrikeModel.CollectionName, services)
    { }

    /// <summary>
    /// Fetch a document by <see cref="GuildConfigWarnStrikeModel.Id"/> as <paramref name="id"/>
    /// </summary>
    /// <param name="id"><see cref="GuildConfigWarnStrikeModel.Id"/></param>
    /// <returns>null when not found</returns>
    public async Task<GuildConfigWarnStrikeModel?> Get(string id)
    {
        var filter = Builders<GuildConfigWarnStrikeModel>
            .Filter
            .Where(v => v.Id == id);
        var res = await BaseFind(filter, limit: 1);
        return res?.FirstOrDefault();
    }
    
    /// <summary>
    /// Fetch a document by it's GuildId.
    /// </summary>
    /// <param name="guildId"><see cref="GuildConfigWarnStrikeModel.GuildId"/></param>
    /// <returns>null when not found</returns>
    public async Task<GuildConfigWarnStrikeModel?> GetByGuild(ulong guildId)
    {
        var filter = Builders<GuildConfigWarnStrikeModel>
            .Filter
            .Where(v => v.GuildId == guildId);
        var res = await BaseFind(filter, limit: 1);
        return res.FirstOrDefault();
    }

    /// <summary>
    /// <para>Insert or Update a document. Uses <see cref="GuildConfigWarnStrikeModel.Id"/> as PK</para>
    ///
    /// <para>Also updates <see cref="GuildConfigWarnStrikeModel.UpdatedAt"/> to the current Unix Time (UTC, Seconds)</para>
    /// </summary>
    /// <returns>Model with <see cref="GuildConfigWarnStrikeModel.UpdatedAt"/> changed</returns>
    /// <exception cref="NoNullAllowedException">Thrown when <see cref="BaseRepository{TH}.GetCollection()"/> results in `null`</exception>
    public async Task<GuildConfigWarnStrikeModel> InsertOrUpdate(GuildConfigWarnStrikeModel model)
    {
        var filter = Builders<GuildConfigWarnStrikeModel>
            .Filter
            .Where(v => v.Id == model.Id);
        var existsRes = await BaseFind(filter);

        model.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        if (await existsRes.AnyAsync())
        {
            await collection.ReplaceOneAsync(filter, model);
        }
        else
        {
            await collection.InsertOneAsync(model);
        }

        return model;
        
    }
}