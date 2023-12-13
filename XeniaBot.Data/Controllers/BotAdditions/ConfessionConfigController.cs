using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using XeniaBot.Data.Helpers;
using XeniaBot.Data.Models;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.Data.Controllers.BotAdditions;

[BotController]
public class ConfessionConfigController : BaseConfigController<ConfessionGuildModel>, IFlightCheckValidator
{
    private readonly DiscordSocketClient _client;
    public ConfessionConfigController(IServiceProvider services)
        : base("confesionGuildModel", services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
    }

    public async Task<FlightCheckValidationResult> FlightCheckGuild(SocketGuild guild)
    {
        string flightCheckName = "Confession";
        var data = await GetGuild(guild.Id);
        if (data == null)
            return new FlightCheckValidationResult(true, flightCheckName, "Not configured. Ignoring");

        var issues = new List<string>();
        
        try
        {
            guild.GetChannel((ulong)data.ChannelId);
            if (!DataHelper.CanAccessChannel(_client, guild.GetChannel((ulong)data.ChannelId)))
            {
                issues.Add($"Unable to send messages in {DiscordURLHelper.GuildChannel(guild.Id, (ulong)data.ChannelId)}.");
            }
        }
        catch (Exception ex)
        {
            issues.Add($"Failed to fetch confession channel {DiscordURLHelper.GuildChannel(guild.Id, (ulong)data.ChannelId)} (`{ex.Message}`)");
        }

        try
        {
            guild.GetChannel((ulong)data.ModalChannelId);
            try
            {
                if (data.ModalMessageId != null)
                {
                    await guild.GetTextChannel((ulong)data.ModalChannelId).GetMessageAsync((ulong)data.ModalMessageId);
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Failed to find modal message {DiscordURLHelper.GuildChannelMessage(guild.Id, (ulong)data.ModalChannelId, (ulong)data.ModalMessageId)} (`{ex.Message}`)");
            }
        }
        catch (Exception ex)
        {
            issues.Add($"Failed to fetch modal channel {DiscordURLHelper.GuildChannel(guild.Id, (ulong)data.ModalChannelId)} (`{ex.Message})");
        }

        if (issues.Count > 0)
        {
            return new FlightCheckValidationResult(false, flightCheckName, "Multiple issues detected. Please reconfigure the confession module.", issues: issues);
        }

        return new FlightCheckValidationResult(true, flightCheckName);
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
        if (existingItems != null && existingItems.Any())
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