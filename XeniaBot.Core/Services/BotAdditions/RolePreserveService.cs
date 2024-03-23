using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.Core.Services.BotAdditions;

[XeniaController]
public class RolePreserveService : BaseService
{
    private readonly DiscordSocketClient _client;
    private readonly RolePreserveRepository _config;
    private readonly RolePreserveGuildRepository _guildConfig;
    private readonly ErrorReportService _err;
    private readonly ServerLogRepository _serverLogConfig;
    public RolePreserveService(IServiceProvider services)
        : base(services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        _config = services.GetRequiredService<RolePreserveRepository>();
        _err = services.GetRequiredService<ErrorReportService>();
        _serverLogConfig = services.GetRequiredService<ServerLogRepository>();
        _guildConfig = services.GetRequiredService<RolePreserveGuildRepository>();
        
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

            var ourHighestRoleEnumerable = arg.Guild.CurrentUser.Roles.OrderByDescending(v => v.Position);
            var ourHighestRolePos = ourHighestRoleEnumerable.FirstOrDefault()?.Position ?? int.MinValue;

            var success = new List<ulong>();
            var fail = new List<ulong>();
            foreach (var item in model.Roles)
            {
                if (item == arg.Guild.EveryoneRole.Id)
                    continue;
                try
                {
                    var existingRole = arg.Guild.GetRole(item);
                    if (existingRole.Position > ourHighestRolePos)
                    {
                        fail.Add(item);
                        continue;
                    }
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

    public override Task OnReadyDelay()
    {
        PreserveAll();
        return Task.CompletedTask;
    }

    public async Task PreserveAll()
    {
        try
        {
            var taskList = new List<Task>();
            var chunkedIds = ArrayHelper.Chunk(_client.Guilds.Select(v => v.Id).ToArray(), 10);
            foreach (var x in chunkedIds)
            {
                var outerItem = x;
                taskList.Add(new Task(
                    delegate
                    {
                        foreach (var i in outerItem)
                        {
                            var guild = _client.GetGuild(i);
                            try
                            {
                                PreserveGuild(guild).Wait();
                            }
                            catch (Exception ex)
                            {
                                _err.ReportException(ex, $"Failed to Preserve Guild {guild.Name} ({guild.Id})").Wait();
                            }
                        }
                    }));
            }

            foreach (var i in taskList)
                i.Start();
            await Task.WhenAll(taskList);
        }
        catch (Exception ex)
        {
            await _err.ReportException(ex, $"Failed to preserve all guilds ;w;");
        }
    }
    
    public async Task PreserveGuild(SocketGuild guild)
    {
        var taskList = new List<Task>();
        foreach (var u in guild.Users)
        {
            taskList.Add(new Task(
                delegate
                {
                    PreserveGuildMember(guild.Id, u.Id).Wait();
                }));
        }

        foreach (var i in taskList)
            i.Start();
        await Task.WhenAll(taskList);
        Log.Debug($"Guild {guild.Name} ({guild.Id}) Preserved. ({taskList.Count} members)");
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