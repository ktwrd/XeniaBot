using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using XeniaBot.Data.Helpers;
using XeniaBot.Data.Models;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.Data.Controllers.BotAdditions;

[BotController]
public class ServerLogConfigController : BaseConfigController<ServerLogModel>, IFlightCheckValidator
{
    private readonly DiscordSocketClient _client;
    public ServerLogConfigController(IServiceProvider services)
        : base("serverLogConfig", services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
    }

    public async Task<FlightCheckValidationResult> FlightCheckGuild(SocketGuild guild)
    {
        string flightCheckName = "Server Log";
        var data = await Get(guild.Id);
        if (data == null)
            return new FlightCheckValidationResult(true, flightCheckName, "Not configured. Ignoring");

        var issueList = new List<string>();
        foreach (var pair in data.GetAsDictionary())
        {
            if ((pair.Value ?? 0) == 0)
                continue;

            try
            {
                guild.GetChannel((ulong)pair.Value);
            }
            catch (Exception ex)
            {
                issueList.Add($"Failed to get channel for {pair.Key} {DiscordURLHelper.GuildChannel(guild.Id, (ulong)pair.Value)} (`{ex.Message}`)");
            }
            if (!DataHelper.CanAccessChannel(_client, guild.GetChannel((ulong)pair.Value)))
            {
                issueList.Add($"Unable to send messages in {DiscordURLHelper.GuildChannel(guild.Id, (ulong)pair.Value)} for {pair.Key} event.");
            }
        }

        if (issueList.Count > 0)
        {
            return new FlightCheckValidationResult(
                false, flightCheckName, "Please reconfigure the Server Log feature to resolve all of these issues",
                issueList);
        }

        return new FlightCheckValidationResult(true, flightCheckName);
    }

    public async Task<ServerLogModel?> Get(ulong serverId)
    {
        var collection = GetCollection();
        var filter = Builders<ServerLogModel>
            .Filter
            .Eq("ServerId", serverId);

        var result = await collection.FindAsync(filter);
        var first = await result.FirstOrDefaultAsync();
        if (first == null)
            first = new ServerLogModel()
            {
                ServerId = serverId
            };
        return first;
    }

    public async Task Set(ServerLogModel model)
    {
        var collection = GetCollection();
        var filter = Builders<ServerLogModel>
            .Filter
            .Eq("ServerId", model.ServerId);

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