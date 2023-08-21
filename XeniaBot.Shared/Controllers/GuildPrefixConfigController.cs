using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using XeniaBot.Shared.Models;

namespace XeniaBot.Shared.Controllers;

[BotController]
public class GuildPrefixConfigController : BaseConfigController<GuildPrefixConfigModel>
{
    private readonly ConfigData _config;
    public GuildPrefixConfigController(IServiceProvider services)
        : base("guildPrefix", services)
    {
        _config = services.GetRequiredService<ConfigData>();
    }

    public async Task<string> GetPrefix(ulong? guildId)
    {
        if (guildId == null)
            return _config.Prefix;
        var data = await Get((ulong)guildId);
        return data?.Prefix ?? _config.Prefix;
    }

    public async Task<GuildPrefixConfigModel> Get(ulong guildId)
    {
        var collection = GetCollection();
        var defaultData = new GuildPrefixConfigModel(guildId);
        var result = await collection.FindAsync(defaultData.Filter);
        return result.FirstOrDefault() ?? defaultData;
    }

    public async Task<bool> Exists(ulong guildId)
    {
        var collection = GetCollection();
        var result = await collection.FindAsync(new GuildPrefixConfigModel(guildId).Filter);
        return result?.Any() ?? false;
    }
    public async Task Set(GuildPrefixConfigModel data)
    {
        var collection = GetCollection();
        if (await Exists(data.GuildId))
        {
            await collection.ReplaceOneAsync(data.Filter, data);
        }
        else
        {
            await collection.InsertOneAsync(data);
        }
    }
}