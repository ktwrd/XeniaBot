using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Repositories;

[XeniaController]
public class ConfessionConfigRepository : BaseRepository<ConfessionGuildModel>
{
    private readonly DiscordSocketClient _client;
    public ConfessionConfigRepository(IServiceProvider services)
        : base("confesionGuildModel", services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
    }
    
    public async Task InitializeModal(ulong guildId, ulong channelId, ulong modalChannelId)
    {
        var data = await GetGuild(guildId);
        if (data == null)
        {
            data = new ConfessionGuildModel()
            {
                GuildId = guildId,
                ChannelId = channelId,
                ModalChannelId = modalChannelId
            };
            await Set(data);
        }

        var confessionEmbed = new EmbedBuilder()
        {
            Title = "Confessions",
            Description = $"Add anonymous confession to <#{channelId}>"
        };

        var guild = _client.GetGuild(guildId);
        var channel = guild.GetTextChannel(modalChannelId);
        var components = new ComponentBuilder()
            .WithButton("Confess", "confessioncontroller_confess_button", ButtonStyle.Primary);

        var message = await channel.SendMessageAsync(embed: confessionEmbed.Build(), components: components.Build());
        data.ModalMessageId = message.Id;
        await Set(data);
    }
    
    public async Task<ConfessionGuildModel?> GetGuild(ulong guildId)
    {
        var collection = GetCollection();
        var filter = Builders<ConfessionGuildModel>
            .Filter
            .Eq("GuildId", guildId);

        var results = await collection.FindAsync(filter);

        return results.FirstOrDefault();
    }
    public async Task Set(ConfessionGuildModel model)
    {
        var collection = GetCollection();
        var filter = Builders<ConfessionGuildModel>
            .Filter
            .Eq("GuildId", model.GuildId);

        var existingItems = await collection.FindAsync(filter);
        if (existingItems != null && await existingItems.AnyAsync())
            await collection.FindOneAndReplaceAsync(filter, model);
        else
            await collection.InsertOneAsync(model);
    }
    public async Task Delete(ConfessionGuildModel model)
    {
        var guild = _client.GetGuild(model.GuildId);
        if (guild == null)
            throw new Exception($"Guild {model.GuildId} not found");
        var channel = guild.GetTextChannel(model.ModalChannelId);
        if (channel == null)
            throw new Exception($"Channel {model.ModalChannelId} not found");
        await channel.DeleteMessageAsync(model.ModalMessageId);

        var collection = GetCollection();
        var filter = Builders<ConfessionGuildModel>
            .Filter
            .Eq("GuildId", model.GuildId);

        await collection?.DeleteManyAsync(filter);
    }
}