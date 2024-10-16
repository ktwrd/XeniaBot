﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using System.Threading;

namespace XeniaBot.Core.Services.BotAdditions;

[XeniaController]
public class FlightCheckService : BaseService
{
    private readonly DiscordSocketClient _discord;
    private readonly ConfigData _config;
    private readonly ErrorReportService _errReportService;
    public FlightCheckService(IServiceProvider services)
        : base(services)
    {
        _discord = services.GetRequiredService<DiscordSocketClient>();
        _config = services.GetRequiredService<ConfigData>();
        _errReportService = services.GetRequiredService<ErrorReportService>();
        
        _discord.JoinedGuild += DiscordOnJoinedGuild;
    }

    private async Task DiscordOnJoinedGuild(SocketGuild arg)
    {
        new Thread((ThreadStart)async delegate
        {
            try
            {
                await CheckGuild(arg);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to run {nameof(CheckGuild)} for {arg.Name} ({arg.Id})\n{ex}");
            }
        }).Start();
    }

    public override async Task OnReady()
    {
        if (!_config.RefreshFlightCheckOnStart)
        {
            Log.WriteLine($"Not going to run since {nameof(_config.RefreshFlightCheckOnStart)} is false");
            return;
        }
        var taskList = new List<Task>();
        foreach (var index in Enumerable.Range(0, _discord.Guilds.Count))
        {
            var item = _discord.Guilds.ElementAt(index);
            new Thread((ThreadStart)async delegate
            {
                try
                {
                    await CheckGuild(item);
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to run {nameof(CheckGuild)} for {item.Name} ({item.Id})\n{ex}");
                await _errReportService.ReportException(ex);
                }
            }).Start();
        }

        foreach (var item in taskList)
            item.Start();
        await Task.WhenAll(taskList);
    }

    /// <summary>
    /// Run FlightCheck on a specific guild. Pretty much running unit testing on the server to make sure that everything is okay and working.
    /// </summary>
    private async Task CheckGuild(SocketGuild guild)
    {
        Log.WriteLine($"Running FlightCheck on Guild {guild.Id} \"{guild.Name}\"");

        var embed = new EmbedBuilder()
            .WithTitle("FlightCheck")
            .WithColor(Color.Red)
            .WithCurrentTimestamp();
        
        if (!HasValidPermissions(guild))
        {
            embed.AddField("Permissions Invalid", string.Join(
                "\n", new string[]
                {
                    $"I've encountered some issues in your guild [`{guild.Name}`](https://discord.com/channels/{guild.Id}/{guild.Channels.FirstOrDefault()?.Id ?? 0}), and that I do not have my required permissions.",
                    "",
                    $"To resolve this issue, please re-invite me with [this link](https://discord.com/oauth2/authorize?client_id={_discord.CurrentUser.Id}&scope=bot&permissions={_config.InvitePermissions}).",
                    "",
                    "If this does not get resolved, some of Xenia's features will work poorly or it won't work at all. (i.e, server logging, role menu, BanSync, etc...)",
                    "",
                    "Thanks!",
                    "",
                    "P.S; If you have any issues, don't hesitate to chat to the dev team with the discord link in my bio!"
                }));
            Log.WriteLine($"FlightCheck for {guild.Id} \"{guild.Name}\": Permissions invalid");
        }

        try
        {
            if (embed.Fields.Count > 0)
            {
                await guild.Owner.SendMessageAsync(embed: embed.Build());
                Log.WriteLine($"FlightCheck for {guild.Id} \"{guild.Name}\" failed!");
            }
            else
            {
                Log.WriteLine($"FlightCheck for {guild.Id} \"{guild.Name}\" has passed!");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"FlightCheck failed for {guild.Id} {guild.Name}\n{ex}");   
        }
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
        var targetPerms = new GuildPermissions(_config.InvitePermissions);
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