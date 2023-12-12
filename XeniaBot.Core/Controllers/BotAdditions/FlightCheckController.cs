using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Models;
using XeniaBot.Shared;
using XeniaBot.Shared.Controllers;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.Core.Controllers.BotAdditions;

[BotController]
public class FlightCheckController : BaseController, IFlightCheckValidator
{
    private readonly DiscordSocketClient _discord;
    private readonly ConfigData _config;
    private readonly DiscordController _discordCont;
    private readonly IServiceProvider _services;
    public FlightCheckController(IServiceProvider services)
        : base(services)
    {
        _services = services;
        _discord = services.GetRequiredService<DiscordSocketClient>();
        _config = services.GetRequiredService<ConfigData>();
        _discordCont = services.GetRequiredService<DiscordController>();
        
        _discord.JoinedGuild += DiscordOnJoinedGuild;
    }

    private async Task DiscordOnJoinedGuild(SocketGuild arg)
    {
        await CheckGuildInAllValidators(arg);
    }

    public override async Task OnReady()
    {
        var taskList = new List<Task>();
        foreach (var item in _discord.Guilds)
        {
            taskList.Add(new Task(
                delegate
                {
                    CheckGuildInAllValidators(item).Wait();
                }));
        }

        foreach (var item in taskList)
            item.Start();
        await Task.WhenAll(taskList);
    }

    public List<IFlightCheckValidator> FindAllValidators()
    {
        return Program.GetServicesThatExtends<IFlightCheckValidator>();
    }

    public async Task RunGuildFlightCheck(IGuild guild)
    {
        await CheckGuildInAllValidators(_discord.GetGuild(guild.Id));
    }
    
    private async Task CheckGuildInAllValidators(SocketGuild guild)
    {
        Log.WriteLine($"Running FlightCheck on Guild {guild.Id} \"{guild.Name}\"");
        var validators = FindAllValidators();

        var fieldList = new List<EmbedFieldBuilder>();
        foreach (var item in validators)
        {
            var res = await item.FlightCheckGuild(guild);
            if (!res.Success && res.EmbedField != null)
            {
                fieldList.Add(res.EmbedField);
            }
        }
        if (fieldList.Count < 1)
            return;

        var embedList = new List<EmbedBuilder>();
        var chunkedFields = ArrayHelper.Chunk<EmbedFieldBuilder>(fieldList, 10);
        for (int i = 0; i < chunkedFields.Length; i++)
        {
            var groupField = chunkedFields[i];
            var embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithCurrentTimestamp();
            if (i == 0)
            {
                embed.WithTitle("FlightCheck")
                    .WithDescription(string.Join("\n", new string[]
                    {
                        $"Multiple issues have been detected with FlightCheck in your guild [`{guild.Name}`]({DiscordURLHelper.Guild(guild.Id)}).",
                        "",
                        "Please resolve them ASAP for a smooth experience with Xenia"
                    }));
            }

            foreach (var t in groupField)
            {
                embed.AddField(t);
            }
            
            embedList.Add(embed);
        }

        if (embedList.Count < 1)
        {
            Log.WriteLine($"FlightCheck for {guild.Id} \"{guild.Name}\" has passed!");
            return;
        }
        else
        {
            Log.WriteLine($"FlightCheck for {guild.Id} \"{guild.Name}\" failed!");
        }
        foreach (var msgEmbeds in ArrayHelper.Chunk(embedList, 10))
        {
            var built = msgEmbeds.Select(v => v.Build()).ToArray();
            await guild.Owner.SendMessageAsync(embeds: built);
        }
    }

    /// <summary>
    /// Run FlightCheck on a specific guild. Pretty much running unit testing on the server to make sure that everything is okay and working.
    /// </summary>
    public async Task<FlightCheckValidationResult> FlightCheckGuild(SocketGuild guild)
    {
        if (!HasValidPermissions(guild))
        {
            Log.WriteLine($"FlightCheck for {guild.Id} \"{guild.Name}\": Permissions invalid");
            var field = new EmbedFieldBuilder()
                .WithName("Missing Guild Permissions")
                .WithValue("Xenia may not work as intended. " +
                           $"To resolve this, [please re-invite Xenia]({_discordCont.GetInviteLink()}).");
            return new FlightCheckValidationResult(false, field);
        }

        return new FlightCheckValidationResult(true);
    }

    /// <summary>
    /// Check if we have the permissions described in the invite.
    /// </summary>
    /// <param name="guild">Guild to test</param>
    /// <returns>True when we have the correct permissions. False is when permissions are invalid and we need to notify the owner to re-invite the bot.</returns>
    private bool HasValidPermissions(SocketGuild guild)
    {
        // Check if we have the correct permissions
        var selfMember = guild.GetUser(_discord.CurrentUser.Id);
        var targetPerms = new GuildPermissions(_config.Invite_Permissions);
        var permissions = selfMember.GuildPermissions.ToList();
        
        // Do we have the specified permission in the guild?
        var pairDict = new Dictionary<GuildPermission, bool>();
        foreach (var i in targetPerms.ToList())
        {
            bool hasPermission = false;
            foreach (var x in permissions)
            {
                if (hasPermission == true)
                    continue;
                if (x == i)
                {
                    hasPermission = true;
                }
            }

            pairDict.TryAdd(i, hasPermission);
        }

        return pairDict.All(v => v.Value);
    }
}