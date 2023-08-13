using Discord;
using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Controllers.BotAdditions;

public class CounterConfigController : BaseConfigController<CounterGuildModel>
{
    public Dictionary<ulong, ulong> CachedItems { get; set; }

    public CounterConfigController(IServiceProvider services)
        : base("countingGuildModel", services)
    {
        CachedItems = new Dictionary<ulong, ulong>();
    }
    public async Task Set(CounterGuildModel model)
    {
        var collection = GetCollection<CounterGuildModel>();

        var filter = Builders<CounterGuildModel>
            .Filter
            .Eq("GuildId", model.GuildId);

        if (collection?.Find(filter).Any() ?? false)
        {
            await collection?.ReplaceOneAsync(filter, model);
        }
        else
        {
            await collection?.InsertOneAsync(model);
        }
        if (!CachedItems.ContainsKey(model.ChannelId))
            CachedItems.Add(model.ChannelId, model.Count);
        CachedItems[model.ChannelId] = model.Count;
    }
    #region Get
    /// <returns><see cref="null"/> when doesn't exist</returns>
    public async Task<CounterGuildModel?> Get(IGuild guild)
    {
        var collection = GetCollection<CounterGuildModel>();

        var filter = Builders<CounterGuildModel>
            .Filter
            .Eq("GuildId", guild.Id);

        var result = await collection.FindAsync(filter);
        return result.FirstOrDefault();
    }
    public async Task<CounterGuildModel> Get<T>(IGuild guild, T channel) where T : IChannel
    {
        var collection = GetCollection<CounterGuildModel>();

        var filter = Builders<CounterGuildModel>
            .Filter
            .Eq("GuildId", guild.Id);

        var result = await collection.FindAsync(filter);
        var filtered = result.ToList().Where(v => v.ChannelId == channel.Id);
        return filtered.FirstOrDefault() ?? new CounterGuildModel(channel, guild);
    }
    public CounterGuildModel Get<T>(T channel) where T : IChannel
    {
        var collection = GetCollection<CounterGuildModel>();

        var filter = Builders<CounterGuildModel>
            .Filter
            .Eq("ChannelId", channel.Id);

        return collection.Find(filter).FirstOrDefault();
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
    public async Task<CounterGuildModel[]> GetAll()
    {
        var collection = GetCollection<CounterGuildModel>();
        var filter = Builders<CounterGuildModel>
            .Filter.Empty;
        var result = await collection.FindAsync(filter);
        return result.ToList().ToArray();
    }
    public async Task<CounterGuildModel[]> GetAll(IChannel channel)
    {
        var collection = GetCollection<CounterGuildModel>();
        var filter = Builders<CounterGuildModel>
            .Filter
            .Eq("ChannelId", channel.Id);

        var result = await collection.FindAsync(filter);
        return result.ToList().ToArray();
    }
    public async Task<CounterGuildModel[]> GetAll(IGuild guild)
    {
        var collection = GetCollection<CounterGuildModel>();
        var filter = Builders<CounterGuildModel>
        .Filter
            .Eq("GuildId", guild.Id);

        var result = await collection.FindAsync(filter);
        return result.ToList().ToArray();
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
        var filter = Builders<CounterGuildModel>
            .Filter
            .Eq("ChannelId", channelId);
        await collection?.DeleteManyAsync(filter);
    }
    public async Task Delete(IGuild guild)
    {
        var collection = GetCollection<CounterGuildModel>();
        var filter = Builders<CounterGuildModel>
        .Filter
            .Eq("GuildId", guild.Id);
        await collection?.DeleteManyAsync(filter);
    }
    #endregion
}