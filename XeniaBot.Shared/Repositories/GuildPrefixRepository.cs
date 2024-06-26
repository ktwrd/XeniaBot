﻿using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using XeniaBot.Shared.Models;

namespace XeniaBot.Shared.Repositories;

[XeniaController]
public class GuildPrefixRepository : BaseRepository<GuildPrefixConfigModel>
{
    private readonly ConfigData _config;
    public GuildPrefixRepository(IServiceProvider services)
        : base("guildPrefix", services)
    {
        _config = services.GetRequiredService<ConfigData>();
    }

    /// <exception cref="NoNullAllowedException">Thrown when <see cref="BaseRepository{TH}.GetCollection()"/> returns null</exception>
    public async Task<string> GetPrefix(ulong? guildId)
    {
        if (guildId == null)
            return _config.Prefix;
        var data = await Get((ulong)guildId);
        return data?.Prefix ?? _config.Prefix;
    }

    /// <exception cref="NoNullAllowedException">Thrown when <see cref="BaseRepository{TH}.GetCollection()"/> returns null</exception>
    public async Task<GuildPrefixConfigModel> Get(ulong guildId)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var defaultData = new GuildPrefixConfigModel(guildId);
        var result = await collection.FindAsync(defaultData.Filter);
        return result.FirstOrDefault() ?? defaultData;
    }

    /// <summary>
    /// Check if a document exists by it's GuildId
    /// </summary>
    /// <param name="guildId"><see cref="GuildPrefixConfigModel.GuildId"/></param>
    /// <returns>If it exists or not</returns>
    /// <exception cref="NoNullAllowedException">Thrown when <see cref="BaseRepository{TH}.GetCollection()"/> returns null</exception>
    public async Task<bool> Exists(ulong guildId)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
        var result = await collection.FindAsync(new GuildPrefixConfigModel(guildId).Filter);
        return await result.AnyAsync();
    }
    /// <exception cref="NoNullAllowedException">Thrown when <see cref="BaseRepository{TH}.GetCollection()"/> returns null</exception>
    public async Task Set(GuildPrefixConfigModel data)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException("GetCollection resulted in null");
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