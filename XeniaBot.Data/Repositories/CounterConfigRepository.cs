using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Repositories;

[XeniaController]
public class CounterConfigRepository : BaseRepository<CounterGuildModel>
{
    public Dictionary<ulong, ulong> CachedItems { get; set; }

    public CounterConfigRepository(IServiceProvider services)
        : base("countingGuildModel", services)
    {
        CachedItems = new Dictionary<ulong, ulong>();
    }
    public async Task Set(CounterGuildModel model)
    {
        var collection = GetCollection<CounterGuildModel>();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");

        var filter = Builders<CounterGuildModel>
            .Filter
            .Where(e => e.GuildId == model.GuildId);

        var existsRes = await collection.FindAsync(filter);
        
        if (await existsRes.AnyAsync())
        {
            await collection.ReplaceOneAsync(filter, model);
        }
        else
        {
            await collection.InsertOneAsync(model);
        }
        if (!CachedItems.ContainsKey(model.ChannelId))
            CachedItems.Add(model.ChannelId, model.Count);
        CachedItems[model.ChannelId] = model.Count;
    }
    #region Get
    /// <returns><see cref="null"/> when doesn't exist</returns>
    public async Task<CounterGuildModel?> Get(IGuild guild)
    {
        var filter = Builders<CounterGuildModel>
            .Filter
            .Where(e => e.GuildId == guild.Id);

        var result = await BaseFind(filter, limit: 1);
        return result.FirstOrDefault();
    }
    public async Task<CounterGuildModel> Get<T>(IGuild guild, T channel) where T : IChannel
    {
        var filter = Builders<CounterGuildModel>
            .Filter
            .Where(e => e.GuildId == guild.Id && e.ChannelId == channel.Id);

        var result = await BaseFind(filter, limit: 1);
        return result.FirstOrDefault() ?? new CounterGuildModel(channel, guild);
    }
    public async Task<CounterGuildModel> Get<T>(T channel) where T : IChannel
    {
        var collection = GetCollection<CounterGuildModel>();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");

        var filter = Builders<CounterGuildModel>
            .Filter
            .Where(e => e.ChannelId == channel.Id);

        var result = await BaseFind(filter, limit: 1);

        return result.FirstOrDefault();
    }
    public async Task<CounterGuildModel?> GetOrCreate(IGuild guild, IChannel channel)
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
    public async Task<ICollection<CounterGuildModel>> GetAll()
    {
        var filter = Builders<CounterGuildModel>
            .Filter.Empty;
        var result = await BaseFind(filter);
        return result.ToList();
    }
    public async Task<ICollection<CounterGuildModel>> GetAll(IChannel channel)
    {
        var filter = Builders<CounterGuildModel>
            .Filter
            .Where(e => e.ChannelId == channel.Id);

        var result = await BaseFind(filter);
        return result.ToList();
    }
    public async Task<ICollection<CounterGuildModel>> GetAll(IGuild guild)
    {
        var filter = Builders<CounterGuildModel>
        .Filter
            .Where(e => e.GuildId == guild.Id);

        var result = await BaseFind(filter);
        return result.ToList();
    }
    #endregion
    #region Delete
    public async Task Delete(CounterGuildModel model)
    {
        await Delete(model.ChannelId);
    }
    public async Task Delete<T>(T channel) where T : IChannel
    {
        await Delete(channel.Id);
    }
    public async Task Delete(ulong channelId)
    {
        var collection = GetCollection<CounterGuildModel>();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var filter = Builders<CounterGuildModel>
            .Filter
            .Where(e => e.ChannelId == channelId);
        await collection.DeleteManyAsync(filter);
    }
    public async Task Delete(IGuild guild)
    {
        var collection = GetCollection<CounterGuildModel>();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var filter = Builders<CounterGuildModel>
            .Filter
            .Where(e => e.GuildId == guild.Id);
        await collection.DeleteManyAsync(filter);
    }
    #endregion
}