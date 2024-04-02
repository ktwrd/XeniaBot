using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using XeniaBot.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;
using XeniaBot.Shared.Services;

namespace XeniaBot.Data.Services
{
    [XeniaController]
    public class BanSyncService : BaseService
    {
        private readonly DiscordSocketClient _client;
        private readonly BanSyncConfigRepository _guildConfigRepo;
        private readonly ConfigData _configData;
        private readonly BanSyncInfoRepository _banInfoRepo;
        private readonly ErrorReportService _err;
        public BanSyncService(IServiceProvider services)
            : base(services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
            _guildConfigRepo = services.GetRequiredService<BanSyncConfigRepository>();
            _configData = services.GetRequiredService<ConfigData>();
            _banInfoRepo = services.GetRequiredService<BanSyncInfoRepository>();
            _err = services.GetRequiredService<ErrorReportService>();

            var programDetails = services.GetRequiredService<ProgramDetails>();

            if (programDetails.Platform != XeniaPlatform.WebPanel)
            {
                _client.UserJoined += _client_UserJoined;
                _client.UserBanned += _client_UserBanned;
            }
        }

        public override async Task OnReady()
        {
            var taskList = new List<Task>();
            foreach (var item in _client.Guilds)
            {
                var guildId = item.Id;
                taskList.Add(new Task(
                    delegate
                    {
                        var guild = _client.GetGuild(guildId);
                        try
                        {
                            RefreshBans(item).Wait();
                        }
                        catch (Exception ex)
                        {
                            _err.ReportException(ex, $"Failed to run RefreshBans on {guild.Name} {guild.Id}").Wait();
                        }
                    }));
            }

            foreach (var i in taskList)
                i.Start();
            await Task.WhenAll(taskList);
        }

        public Task RefreshBans(ulong guildId) => RefreshBans(_client.GetGuild(guildId));
        public async Task RefreshBans(SocketGuild guild)
        {
            var config = await _guildConfigRepo.Get(guild.Id);
            if ((config?.Enable ?? false) == false || (config?.State ?? BanSyncGuildState.Unknown) != BanSyncGuildState.Active)
                return;

            var bans = await guild.GetBansAsync(1000000).FlattenAsync().ConfigureAwait(false);
            foreach (var i in bans)
            {
                try
                {
                    // dont add existing ban to db, even if the existing one is ghosted.
                    var existing = await _banInfoRepo.GetInfo(i.User.Id, guild.Id, allowGhost: true);
                    if (existing != null)
                        continue;
                
                    var info = new BanSyncInfoModel()
                    {
                        UserId = i.User.Id,
                        UserName = i.User.Username,
                        UserDiscriminator = i.User.Discriminator,
                        UserDisplayName = i.User.GlobalName,
                        GuildId = guild.Id,
                        GuildName = guild.Name,
                        Reason = i?.Reason ?? "<unknown>"
                    };
                    await _banInfoRepo.SetInfo(info);
                }
                catch (Exception ex)
                {
                    await _err.ReportException(
                        ex,
                        $"Failed to add ban for {i?.User.Username} ({i?.User.Id}) in guild {guild.Name} ({guild.Id})");
                }
            }
        }

        /// <summary>
        /// Add user to database and notify mutual servers. <see cref="NotifyBan(BanSyncInfoModel)"/>
        /// </summary>
        public async Task _client_UserBanned(SocketUser user, SocketGuild guild)
        {
            // Ignore if guild config is disabled
            var config = await _guildConfigRepo.Get(guild.Id);
            if ((config?.Enable ?? false) == false || (config?.State ?? BanSyncGuildState.Unknown) != BanSyncGuildState.Active)
                return;

            var banInfo = await guild.GetBanAsync(user);

            var info = new BanSyncInfoModel()
            {
                UserId = user.Id,
                UserName = user.Username,
                UserDiscriminator = user.Discriminator,
                UserDisplayName = user.GlobalName,
                GuildId = guild.Id,
                GuildName = guild.Name,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Reason = banInfo?.Reason ?? "<unknown>",
            };
            await _banInfoRepo.SetInfo(info);
            await NotifyBan(info);
        }

        /// <summary>
        /// Notify all guilds that the user is in that the user has been banned.
        /// </summary>
        public async Task NotifyBan(BanSyncInfoModel info)
        {
            var taskList = new List<Task>();
            foreach (var guild in _client.Guilds)
            {
                var guildUser = guild.GetUser(info.UserId);
                if (guildUser == null)
                    continue;

                var guildConfig = await _guildConfigRepo.Get(guild.Id);
                if (guildConfig == null || (guildConfig?.State ?? BanSyncGuildState.Unknown) != BanSyncGuildState.Active)
                    continue;
                var guildId = guild.Id;
                taskList.Add(new Task(delegate
                {
                    var textChannel = guild.GetTextChannel(guildConfig!.LogChannel);
                    var embed = new EmbedBuilder()
                    {
                        Title = "User in your server just got banned",
                        Description = $"<@{info.UserId}> just got banned from `{info.GuildName}` at <t:{info.Timestamp}:F>",
                    };
                    embed.AddField("Reason", $"```\n{info.Reason}\n```");
                    textChannel.SendMessageAsync(embed: embed.Build()).Wait();
                    try
                    { }
                    catch (Exception ex)
                    {
                        var g = _client.GetGuild(guildId);
                        _err.ReportException(
                            ex,
                            $"Failed to notify guild {g.Name} ({g.Id}) of user {guildUser.Username} ({guildUser.Id}) ban record.").Wait();
                    }
                }));
            }
            foreach (var i in taskList)
                i.Start();
            await Task.WhenAll(taskList);
        }

        private async Task _client_UserJoined(SocketGuildUser arg)
        {
            var guildConfig = await _guildConfigRepo.Get(arg.Guild.Id);

            // Check if the guild has config stuff setup
            // If not then we just ignore
            if (guildConfig == null)
                return;
                
            if ((guildConfig?.State ?? BanSyncGuildState.Unknown) != BanSyncGuildState.Active)
                return;

            // Check if config channel has been made, if not then ignore
            SocketTextChannel? logChannel = arg.Guild.GetTextChannel(guildConfig!.LogChannel);
            if (logChannel == null)
                return;

            // Check if this user has been banned before, if not then ignore
            var userInfo = (await _banInfoRepo.GetInfoEnumerable(arg.Id)).ToArray();
            if (!userInfo.Any())
                return;

            userInfo = userInfo.Where(v => !v.Ghost).ToArray();

            // Create embed then send message in log channel.
            var embed = await GenerateEmbed(userInfo);
            await logChannel.SendMessageAsync(embed: embed.Build());
        }
        public async Task<EmbedBuilder> GenerateEmbed(ICollection<BanSyncInfoModel> data)
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
            var discriminator = user.Discriminator ?? last?.UserDiscriminator ?? "0000";
            embed.WithDescription($"User {name}#{discriminator} ({user.Id}) has been banned from {sortedData.Length} guilds.");

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
            NotEnoughMembers,
            Blacklisted,
            Valid,
            LogChannelMissing,
            LogChannelCannotAccess
        }
        public async Task<BanSyncGuildKind> GetGuildKind(ulong guildId)
        {
            var guild = _client.GetGuild(guildId);

            var guildConf = await _guildConfigRepo.Get(guildId);
            if ((guildConf?.LogChannel ?? 0) == 0)
                return BanSyncGuildKind.LogChannelMissing;

            try
            { guild.GetTextChannel(guildConf?.LogChannel ?? 0); }
            catch
            { return BanSyncGuildKind.LogChannelCannotAccess; }
            if (guildConf is { State: BanSyncGuildState.Blacklisted })
                return BanSyncGuildKind.Blacklisted;

            if (guild.CreatedAt > DateTimeOffset.UtcNow.AddMonths(-6))
                return BanSyncGuildKind.TooYoung;
            else if (guild.MemberCount < 35)
                return BanSyncGuildKind.NotEnoughMembers;
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
            var config = await _guildConfigRepo.Get(guildId);
            if (config == null)
                return null;

            var oldConfig = await _guildConfigRepo.Get(guildId);

            if (state == BanSyncGuildState.Blacklisted || state == BanSyncGuildState.RequestDenied)
            {
                if (config.Reason.Length < 1)
                    throw new Exception("Reason parameter is required");

                config.Reason = reason;
                config.Enable = false;
            }

            config.Enable = state == BanSyncGuildState.Active;
            config.State = state;

            await _guildConfigRepo.Set(config);
            
            await SetGuildState_Notify(config);
            await SetGuildState_NotifyGuild(config, oldConfig);

            if (state == BanSyncGuildState.Active)
            {
                try
                {
                    await RefreshBans(_client.GetGuild(guildId));
                }
                catch (Exception ex)
                {
                    var guild = _client.GetGuild(guildId);
                    await _err.ReportException(ex, $"Failed to refresh bans in {guild.Name} ({guildId})");
                }
            }

            return config;
        }
        
        protected async Task SetGuildState_Notify(ConfigBanSyncModel model)
        {
            try
            {
                var guild = _client.GetGuild(model.GuildId);
                var logGuild = _client.GetGuild(_configData.BanSync.GuildId);
                var logChannel = logGuild.GetTextChannel(_configData.BanSync.LogChannelId);

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
                    }),
                    Url = _configData.HasDashboard ? $"{_configData.DashboardUrl}/Admin/Server/{guild.Id}#settings" : ""
                }.WithCurrentTimestamp().Build());
            }
            catch (Exception ex)
            {
                await _err.ReportException(ex, $"To notify bot owner about guild state change for {model.GuildId}");
                return;
            }
        }

        /// <summary>
        /// Notify guild owner when the BanSync state for their guild has changed.
        /// </summary>
        /// <param name="current">Current model content in db.</param>
        /// <param name="previous">Previous model in db before the change was made.</param>
        protected async Task SetGuildState_NotifyGuild(ConfigBanSyncModel current, ConfigBanSyncModel previous)
        {
            var guild = _client.GetGuild(current.GuildId);
            var channel = guild.GetTextChannel(current.LogChannel);
            if (channel == null)
            {
                Log.Warn($"Failed to get channel {current.LogChannel} in guild {guild.Id} ({guild.Name})");
                return;
            }

            var embed = new EmbedBuilder()
                .WithCurrentTimestamp()
                .WithTitle("Ban Sync Notification");
            var baseServerMsg = $"<@{guild.OwnerId}> An update about BanSync in your server.";
            var baseDmMsg =
                $"An update about BanSync in your server, [`{guild.Name}`](https://discord.com/channels/{guild.Id}/)";

            var contact =
                $"[join our support server]({_configData.SupportServerUrl})";


            if (current.State == BanSyncGuildState.Active)
            {
                if (previous.State == BanSyncGuildState.RequestDenied)
                {
                    // mind has been changed
                    embed.WithColor(Color.Green)
                        .WithDescription(
                            $"A mistake may have been made by the admins. BanSync has been re-enabled for your guild.");
                }
                else if (previous.State == BanSyncGuildState.Blacklisted)
                {
                    // blacklist removed
                    embed.WithColor(Color.Green)
                        .WithDescription(
                            $"Your guild has been removed from the blacklist and BanSync has been re-enabled.");
                }
                else
                {
                    // bansync added
                    var d =
                        "Congratulations! The BanSync feature was approved for usage in your server. All banned members have been synchronized on our side and you can see members in your server with an existing history on the dashboard.\n" +
                        "\n" +
                        $"If you need any assistance. Feel free to {contact}.";
                    if (_configData.HasDashboard)
                        d += $"\n\nIf you would like to check mutual records in your server, you can do so [via the dashboard]({_configData.DashboardUrl}/Server/{guild.Id}/BanSync)";
                    embed.WithColor(Color.Green)
                        .WithDescription(
                            d);              
                }
            }
            else if (current.State == BanSyncGuildState.Blacklisted)
            {
                if (previous.State == BanSyncGuildState.PendingRequest)
                {
                    // rejected and blacklisted
                    embed.WithColor(Color.Red)
                        .WithDescription(
                            $"Your request to enable BanSync has been rejected and your guild has been blacklisted. For more information, {contact} if you would like to appeal.");
                }
                else if (previous.State != BanSyncGuildState.Blacklisted)
                {
                    // blacklisted
                    embed.WithColor(Color.Red)
                        .WithDescription(
                            $"Your guild has been blacklisted to use the BanSync feature. For more information, {contact} if you would like to appeal.");
                }
            }
            else if (current.State == BanSyncGuildState.RequestDenied)
            {
                if (previous.State != BanSyncGuildState.RequestDenied)
                {
                    // request has been denied
                    embed.WithColor(Color.Red)
                        .WithDescription(
                            $"Your request to enable the BanSync feature has been denied. For more information, {contact}");
                }
            }
            else if (current.State == BanSyncGuildState.PendingRequest)
            {
                if (previous.State != BanSyncGuildState.PendingRequest)
                {
                    // awaiting approval
                    embed.WithColor(Color.Blue)
                        .WithDescription(
                            $"The BanSync feature has been requested for your server. Please wait 24-48hr for our admin team to review your server. \n\n" +
                            $"***If it takes longer than that***, then {contact}.");
                }
            }
            else
            {
                if (previous.State == BanSyncGuildState.Active)
                {
                    // awaiting approval
                    embed.WithColor(Color.Blue)
                        .WithDescription(
                            $"The BanSync feature has been disabled in your server. Please {contact} for more information.");
                }
                else
                {
                    return;
                }
            }

            await channel.SendMessageAsync(
                baseServerMsg, embed: embed.Build());

            await guild.Owner.SendMessageAsync(
                baseDmMsg,
                embed: embed.Build());
        }
        /// <summary>
        /// Request for BanSync to be enabled on the guild specified.
        /// </summary>
        /// <param name="guildId">GuildId to request the BanSync feature on.</param>
        /// <returns>Updated <see cref="ConfigBanSyncModel"/></returns>
        public async Task<ConfigBanSyncModel> RequestGuildEnable(ulong guildId)
        {
            var config = await _guildConfigRepo.Get(guildId);
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

            // Ignore when log channel is missing or we can't access it.
            var guildState = await GetGuildKind(guildId);
            if (guildState == BanSyncGuildKind.LogChannelMissing ||
                guildState == BanSyncGuildKind.LogChannelCannotAccess)
                return config;

            config.State = BanSyncGuildState.PendingRequest;
            await _guildConfigRepo.Set(config);

            await RequestGuildEnable_SendNotification(config);

            config = await _guildConfigRepo.Get(guildId);

            return config;
        }
        /// <summary>
        /// Send notification to <see cref="BanSyncConfigItem.RequestChannelId."/> that a server has requested the BanSync feature.
        /// </summary>
        protected async Task RequestGuildEnable_SendNotification(ConfigBanSyncModel model)
        {
            var guild = _client.GetGuild(model.GuildId);
            var logGuild = _client.GetGuild(_configData.BanSync.GuildId);
            var logRequestChannel = logGuild.GetTextChannel(_configData.BanSync.RequestChannelId);
            // Fetch first text channel to create invite for
            var firstTextChannel = guild.Channels.OfType<ITextChannel>().FirstOrDefault();

            // Generate invite from firstTextChannel and fetch the URL for the invite
            IInviteMetadata? invite = null;
            if (firstTextChannel != null)
                invite = await firstTextChannel.CreateInviteAsync(null);
            string inviteUrl = invite?.Url ?? "none";

            await logRequestChannel.SendMessageAsync(embed: new EmbedBuilder()
            {
                Title = "BanSync Request Received.",
                Description = string.Join("\n", new string[]
                {
                    "```",
                    $"Id: {guild.Id}",
                    $"Name: {guild.Name}",
                    $"Owner: {guild.Owner.Username}#{guild.Owner.Discriminator} ({guild.Owner.Id})",
                    $"Member Count: {guild.MemberCount}",
                    $"Invite: {inviteUrl}",
                    "```"
                }),
                Url = _configData.HasDashboard ? $"{_configData.DashboardUrl}/Admin/Server/{guild.Id}#settings" : ""
            }.Build());
        }
    }
}
