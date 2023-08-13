﻿using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using XeniaBot.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using XeniaBot.Data.Controllers;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Models;

namespace XeniaBot.Data.Controllers.BotAdditions
{
    [BotController]
    public class BanSyncController : BaseController
    {
        private readonly DiscordSocketClient _client;
        private readonly DiscordController _discord;
        private readonly BanSyncConfigController _config;
        private readonly ConfigData _configData;
        private readonly BanSyncInfoConfigController _infoConfig;
        private readonly ProgramDetails _details;
        public BanSyncController(IServiceProvider services)
            : base(services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
            _discord = services.GetRequiredService<DiscordController>();
            _config = services.GetRequiredService<BanSyncConfigController>();
            _configData = services.GetRequiredService<ConfigData>();
            _infoConfig = services.GetRequiredService<BanSyncInfoConfigController>();
            _details = services.GetRequiredService<ProgramDetails>();

            if (_details.Platform != XeniaPlatform.WebPanel)
            {
                _client.UserJoined += _client_UserJoined;
                _client.UserUnbanned += _client_UserUnbanned;
                _client.UserBanned += _client_UserBanned;
            }
        }

        public override Task InitializeAsync() => Task.CompletedTask;

        /// <summary>
        /// Add user to database and notify mutual servers. <see cref="NotifyBan(BanSyncInfoModel)"/>
        /// </summary>
        public async Task _client_UserBanned(SocketUser user, SocketGuild guild)
        {
            // Ignore if guild config is disabled
            var config = await _config.Get(guild.Id);
            if ((config?.Enable ?? false) == false)
                return;

            var banInfo = await guild.GetBanAsync(user);

            var info = new BanSyncInfoModel()
            {
                UserId = user.Id,
                UserName = user.Username,
                UserDiscriminator = user.Discriminator.ToString(),
                GuildId = guild.Id,
                GuildName = guild.Name,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Reason = banInfo?.Reason ?? "null",
            };
            await _infoConfig.SetInfo(info);
            await NotifyBan(info);
        }

        /// <summary>
        /// Notify all guilds that the user is in that the user has been banned.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public async Task NotifyBan(BanSyncInfoModel info)
        {
            var taskList = new List<Task>();
            foreach (var guild in _client.Guilds)
            {
                var guildUser = guild.GetUser(info.UserId);
                if (guildUser == null)
                    continue;

                var guildConfig = await _config.Get(guild.Id);
                if (guildConfig == null)
                    continue;
                taskList.Add(new Task(async delegate
                {
                    var textChannel = guild.GetTextChannel(guildConfig.LogChannel);
                    var embed = new EmbedBuilder()
                    {
                        Title = "User in your server just got banned",
                        Description = $"<@{info.UserId}> just got banned from `{guild.Name}` at <t:{info.Timestamp}:F>",
                    };
                    embed.AddField("Reason", $"```\n{info.Reason}\n```");
                    await textChannel.SendMessageAsync(embed: embed.Build());
                }));
            }
            foreach (var i in taskList)
                i.Start();
            await Task.WhenAll(taskList);
        }

        /// <summary>
        /// Remove user from the database if they exist
        /// </summary>
        public async Task _client_UserUnbanned(SocketUser user, SocketGuild guild)
        {
            // Ignore if guild config is disabled
            var config = await _config.Get(guild.Id);
            if ((config?.Enable ?? false) == false)
                return;

            await _infoConfig.RemoveInfo(user.Id, guild.Id);
        }

        private async Task _client_UserJoined(SocketGuildUser arg)
        {
            var guildConfig = await _config.Get(arg.Guild.Id);

            // Check if the guild has config stuff setup
            // If not then we just ignore
            if (guildConfig == null)
                return;

            // Check if config channel has been made, if not then ignore
            SocketTextChannel? logChannel = arg.Guild.GetTextChannel(guildConfig.LogChannel);
            if (logChannel == null)
                return;

            // Check if this user has been banned before, if not then ignore
            var userInfo = (await _infoConfig.GetInfoEnumerable(arg.Id)).ToArray();
            if (!userInfo.Any())
                return;

            // Create embed then send message in log channel.
            var embed = await GenerateEmbed(userInfo);
            await logChannel.SendMessageAsync(embed: embed.Build());
        }
        public async Task<EmbedBuilder> GenerateEmbed(IEnumerable<BanSyncInfoModel> data)
        {
            var sortedData = data.OrderByDescending(v => v.Timestamp).ToArray();
            var last = sortedData.LastOrDefault();
            var userId = last?.UserId;
            var user = await _client.GetUserAsync(userId ?? 0);
            var embed = new EmbedBuilder()
            {
                Title = "User has been banned previously",
                Color = Color.Red
            };
            var name = user.Username ?? last?.UserName ?? "<Unknown Username>";
            var discrim = user.Discriminator ?? last?.UserDiscriminator ?? "0000";
            embed.WithDescription($"User {name}#{discrim} ({user.Id}) has been banned from {sortedData.Length} guilds.");

            for (int i = 0; i < Math.Min(sortedData.Length, 25); i++)
            {
                var item = sortedData[i];

                embed.AddField(
                    item.GuildName,
                    string.Join("\n", new string[] {
                        "```",
                        item.Reason,
                        "```",
                        $"<t:{item.Timestamp}:F>"
                    }),
                    true);
            }

            return embed;
        }

        public enum BanSyncGuildKind
        {
            TooYoung,
            NotEnougMembers,
            Blacklisted,
            Valid
        }
        public BanSyncGuildKind GetGuildKind(ulong guildId)
        {
            var guild = _client.GetGuild(guildId);

            if (guild.CreatedAt > DateTimeOffset.UtcNow.AddMonths(-6))
                return BanSyncGuildKind.TooYoung;
            else if (guild.MemberCount < 50)
                return BanSyncGuildKind.NotEnougMembers;
            else
                return BanSyncGuildKind.Valid;
        }
        /// <summary>
        /// Set guild state and write to log channel.
        /// </summary>
        /// <param name="guildId">Target Guild snowflake</param>
        /// <param name="state">New state for the guild config</param>
        /// <param name="reason">Required when <paramref name="state"/> is <see cref="BanSyncGuildState.Blacklisted"/> or <see cref="BanSyncGuildState.RequestDenied"/></param>
        /// <returns></returns>
        /// <exception cref="Exception">When <paramref name="reason"/> is empty when required.</exception>
        public async Task<ConfigBanSyncModel?> SetGuildState(ulong guildId, BanSyncGuildState state, string reason = "")
        {
            var config = await _config.Get(guildId);
            if (config == null)
                return null;

            if (state == BanSyncGuildState.Blacklisted || state == BanSyncGuildState.RequestDenied)
            {
                if (config.Reason.Length < 1)
                    throw new Exception("Reason parameter is required");

                config.Reason = reason;
            }

            await SetGuildState_Notify(config);

            await _config.Set(config);
            return config;
        }
        protected async Task SetGuildState_Notify(ConfigBanSyncModel model)
        {
            var guild = _client.GetGuild(model.GuildId);
            var logGuild = _client.GetGuild(_configData.BanSync_AdminServer);
            var logChannel = logGuild.GetTextChannel(_configData.BanSync_GlobalLogChannel);

            await logChannel.SendMessageAsync(embed: new EmbedBuilder()
            {
                Title = "SetGuildState",
                Description = string.Join("\n", new string[]
                {
                    "```",
                    $"Guild: {guild.Name ?? "<null>"} ({model.GuildId})",
                    $"State: {model.State}",
                    $"Reason: {model.Reason}",
                    "```"
                })
            }.WithCurrentTimestamp().Build());
        }
        public async Task<ConfigBanSyncModel> RequestGuildEnable(ulong guildId)
        {
            var config = await _config.Get(guildId);
            if (config == null)
            {
                config = new ConfigBanSyncModel()
                {
                    GuildId = guildId
                };
            }
            // When state is blacklisted/denied/pending, reject
            if (config.State == BanSyncGuildState.Blacklisted || config.State == BanSyncGuildState.RequestDenied || config.State == BanSyncGuildState.PendingRequest)
            {
                return config;
            }

            config.State = BanSyncGuildState.PendingRequest;
            await _config.Set(config);

            await RequestGuildEnable_SendNotification(config);

            return config;
        }
        protected async Task RequestGuildEnable_SendNotification(ConfigBanSyncModel model)
        {
            var guild = _client.GetGuild(model.GuildId);
            var logGuild = _client.GetGuild(_configData.BanSync_AdminServer);
            var logRequestChannel = logGuild.GetTextChannel(_configData.BanSync_RequestChannel);
            // Fetch first text channel to create invite for
            var firstTextChannel = guild.Channels.OfType<ITextChannel>().FirstOrDefault();

            // Generate invite from firstTextChannel and fetch the URL for the invite
            IInviteMetadata? invite = null;
            if (firstTextChannel != null)
                invite = await firstTextChannel.CreateInviteAsync(null);
            string inviteUrl = invite?.Url ?? "none";

            await logRequestChannel.SendMessageAsync(embed: new EmbedBuilder()
            {
                Description = string.Join("\n", new string[]
                {
                    "```",
                    $"Id: {guild.Id}",
                    $"Name: {guild.Name}",
                    $"Owner: {guild.Owner.Username}#{guild.Owner.Discriminator} ({guild.Owner.Id})",
                    $"Member Count: {guild.MemberCount}",
                    $"Invite: {inviteUrl}",
                    "```"
                })
            }.Build());
        }
    }
}