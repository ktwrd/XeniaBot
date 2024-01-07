using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Models;
using XeniaBot.Shared;
using XeniaBot.Shared.Controllers;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.Core.Controllers.BotAdditions;

[BotController]
public class RolePreserveController : BaseController
{
    private readonly DiscordSocketClient _client;
    private readonly RolePreserveConfigController _config;
    private readonly RolePreserveGuildConfigController _guildConfig;
    private readonly ErrorReportController _err;
    private readonly ServerLogConfigController _serverLogConfig;
    public RolePreserveController(IServiceProvider services)
        : base(services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        _config = services.GetRequiredService<RolePreserveConfigController>();
        _err = services.GetRequiredService<ErrorReportController>();
        _serverLogConfig = services.GetRequiredService<ServerLogConfigController>();
        _guildConfig = services.GetRequiredService<RolePreserveGuildConfigController>();
        
        _client.GuildMemberUpdated += (cacheable, user) => PreserveGuildMember(user.Guild.Id, user.Id);
        _client.UserJoined += ClientOnUserJoined;
    }

    private async Task ClientOnUserJoined(SocketGuildUser arg)
    {
        try
        {
            var guildModel = await _guildConfig.Get(arg.Guild.Id);
            if (guildModel == null || guildModel.Enable == false)
                return;
            var model = await _config.Get(arg.Id, arg.Guild.Id);
            if (model == null)
                return;

            var success = new List<ulong>();
            var fail = new List<ulong>();
            foreach (var item in model.Roles)
            {
                try
                {
                    await arg.AddRoleAsync(item);
                    success.Add(item);
                }
                catch (Exception ex)
                {
                    _err.ReportException(
                        ex,
                        $"Failed to grant role {item} in guild {arg.Guild.Name} ({arg.Guild.Id}) for {arg.Username} ({arg.Id})");
                    fail.Add(item);
                }
            }
            try
            {
                var serverLogModel = await _serverLogConfig.Get(arg.Guild.Id);
                if (serverLogModel == null)
                    return;

                var channelId = serverLogModel.GetChannel(ServerLogEvent.MemberJoin);
                var channel = arg.Guild.GetTextChannel(channelId);

                var embed = new EmbedBuilder()
                    .WithDescription($"Added {success.Count} roles successfully.")
                    .WithTitle($"Role Preserve - User Joined")
                    .WithColor(new Color(255, 255, 255))
                    .WithCurrentTimestamp();
                if (fail.Count > 0)
                    embed.Description += $"\nFailed to add {fail.Count} roles.";

                var f = string.Join("\n", fail.Select(v => $"- <@&{v}>"));
                if (fail.Count > 0)
                {
                    if (f.Length > 2000)
                    {
                        var stream = new MemoryStream(Encoding.UTF8.GetBytes(f));
                        await channel.SendFileAsync(stream, filename: "failed.txt", embed: embed.Build());
                    }
                    else
                    {
                        embed.AddField($"Failed to add following roles", f);
                        await channel.SendMessageAsync(embed: embed.Build());
                    }
                }
            }
            catch (Exception ex)
            {
                await _err.ReportException(
                    ex, $"Failed to notify guild {arg.Guild.Name} ({arg.Guild.Id}) about Role Preserve being actioned.");
            }
        }
        catch (Exception ex)
        {
            await _err.ReportException(
                ex,
                $"Failed to run Role Preserve on user {arg.Username} ({arg.Id}) in guild {arg.Guild.Name} ({arg.Guild.Id})");
        }
    }

    public override async Task OnReady()
    {
        try
        {
            var taskList = new List<Task>();
            foreach (var item in _client.Guilds)
            {
                var id = item.Id;
                taskList.Add(new Task(
                    delegate
                    {
                        var guild = _client.GetGuild(id);
                        try
                        {
                            PreserveGuild(guild).Wait();
                        }
                        catch (Exception ex)
                        {
                            _err.ReportException(ex, $"Failed to Preserve Guild {item.Name} ({item.Id})").Wait();
                        }
                    }));
            }

            foreach (var i in taskList)
                i.Start();
            await Task.WhenAll(taskList);
        }
        catch (Exception ex)
        {
            await _err.ReportException(ex, $"Failed to run OnReady task ;w;");
        }
    }
    
    public async Task PreserveGuild(SocketGuild guild)
    {
        var taskList = new List<Task>();
        foreach (var u in guild.Users)
        {
            taskList.Add(PreserveGuildMember(guild.Id, u.Id));
        }

        foreach (var i in taskList)
            i.Start();
        await Task.WhenAll(taskList);
    }

    public async Task PreserveGuildMember(ulong guildId, ulong userId)
    {
        try
        {
            var guild = _client.GetGuild(guildId);
            var member = guild.GetUser(userId);

            var roleIdList = member.Roles.Select(v => v.Id);

            var model = new RolePreserveModel()
            {
                UserId = userId,
                GuildId = guildId,
                Roles = roleIdList.ToList()
            };
            await _config.Set(model);
        }
        catch (Exception ex)
        {
            await _err.ReportException(ex, $"Failed to preserve member {userId} in guild {guildId}");
        }
    }
}