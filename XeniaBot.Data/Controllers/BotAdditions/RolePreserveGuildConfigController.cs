﻿using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Controllers.BotAdditions;

[BotController]
public class RolePreserveGuildController : BaseConfigController<RolePreserveGuildModel>
{
    public RolePreserveGuildController(IServiceProvider services)
        : base(RolePreserveGuildModel.CollectionName, services)
    {}

    
    public async Task<RolePreserveGuildModel?> Get(ulong guildId)
    {
        var collection = GetCollection();
        var filter = Builders<RolePreserveGuildModel>
            .Filter
            .Eq("GuildId", guildId);
        var res = await collection.FindAsync(filter);
        return res.FirstOrDefault();
    }
    public async Task Set(RolePreserveGuildModel model)
    {
        var collection = GetCollection();
        var filter = Builders<RolePreserveGuildModel>
            .Filter
            .Eq("GuildId", model.GuildId);

        var existResult = await collection.FindAsync(filter);
        var exists = existResult.Any();

        if (exists)
        {
            await collection.FindOneAndReplaceAsync(filter, model);
        }
        else
        {
            await collection.InsertOneAsync(model);
        }
    }
}