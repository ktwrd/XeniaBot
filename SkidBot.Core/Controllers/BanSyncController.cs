using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using SkidBot.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SkidBot.Core.Controllers
{
    public class BanSyncController
    {
        private readonly DiscordSocketClient _client;
        private readonly DiscordController _discord;
        private readonly BanSyncConfigController _config;
        private readonly IServiceProvider _services;
        public BanSyncController(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
            _discord = services.GetRequiredService<DiscordController>();
            _config = services.GetRequiredService<BanSyncConfigController>();
            _services = services;

            _client.UserJoined += _client_UserJoined;
            _client.UserUnbanned += _client_UserUnbanned;
            _client.UserBanned += _client_UserBanned;
            Log.Debug("hi");
        }

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
            await SetInfo(info);
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

            await RemoveInfo(user.Id, guild.Id);
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
            var userInfo = (await GetInfoEnumerable(arg.Id)).ToArray();
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

        #region Mongo Helpers
        public const string MongoInfoCollectionName = "banSyncInfo";
        protected IMongoCollection<T>? GetInfoCollection<T>()
        {
            return Program.GetMongoDatabase()?.GetCollection<T>(MongoInfoCollectionName);
        }
        protected IMongoCollection<BanSyncInfoModel>? GetInfoCollection()
            => GetInfoCollection<BanSyncInfoModel>();
        protected async Task<IEnumerable<T>> BaseInfoFind<T>(FilterDefinition<T> data)
        {
            var collection = GetInfoCollection<T>();
            var result = await collection.FindAsync(data);

            return result.ToEnumerable();
        }
        protected async Task<bool> BaseInfoAny<T>(FilterDefinition<T> data)
        {
            var collection = GetInfoCollection<T>();
            var result = await collection.FindAsync(data);

            return result.Any();
        }
        protected async Task<T?> BaseInfoFirstOrDefault<T>(FilterDefinition<T> data)
        {
            var collection = GetInfoCollection<T>();
            var result = await collection.FindAsync(data);

            return result.FirstOrDefault();
        }
        #endregion

        #region Get/Set
        #region Get
        public async Task<IEnumerable<BanSyncInfoModel>> GetInfoEnumerable(ulong userId, ulong guildId)
        {
            var filter = Builders<BanSyncInfoModel>
                .Filter
                .Where(v => v.UserId == userId && v.GuildId == guildId);

            return await BaseInfoFind(filter);
        }
        public async Task<IEnumerable<BanSyncInfoModel>> GetInfoEnumerable(BanSyncInfoModel data)
            => await GetInfoEnumerable(data.UserId, data.GuildId);
        public async Task<IEnumerable<BanSyncInfoModel>> GetInfoEnumerable(ulong userId)
        {
            var filter = Builders<BanSyncInfoModel>
                .Filter
                .Eq("UserId", userId);

            return await BaseInfoFind(filter);
        }
        public async Task<BanSyncInfoModel?> GetInfo(ulong userId, ulong guildId)
        {
            var filter = Builders<BanSyncInfoModel>
                .Filter
                .Where(v => v.UserId == userId && v.GuildId == guildId);

            return await BaseInfoFirstOrDefault(filter);
        }
        public async Task<BanSyncInfoModel?> GetInfo(BanSyncInfoModel data)
            => await GetInfo(data.UserId, data.GuildId);
        #endregion

        public async Task SetInfo(BanSyncInfoModel data)
        {
            var collection = GetInfoCollection();
            var filter = Builders<BanSyncInfoModel>
                .Filter
                .Where(v => v.UserId == data.UserId && v.GuildId == data.GuildId);
            if (await InfoExists(data))
            {
                await collection.ReplaceOneAsync(filter, data);
            }
            else
            {
                await collection.InsertOneAsync(data);
            }
        }
        public async Task RemoveInfo(ulong userId, ulong guildId)
        {
            var collection = GetInfoCollection();
            var filter = Builders<BanSyncInfoModel>
                .Filter
                .Where(v => v.UserId == userId && v.GuildId == guildId);

            await collection.DeleteManyAsync(filter);
        }
        #endregion

        #region Info Exists
        public async Task<bool> InfoExists(ulong userId, ulong guildId)
        {
            var filter = Builders<BanSyncInfoModel>
                .Filter
                .Where(v => v.UserId == userId && v.GuildId == guildId);
            return await BaseInfoAny(filter);
        }
        public async Task<bool> InfoExists(ulong userId)
        {
            var filter = Builders<BanSyncInfoModel>
                .Filter
                .Eq("UserId", userId);
            return await BaseInfoAny(filter);
        }
        public async Task<bool> InfoExists(BanSyncInfoModel data)
            => await InfoExists(data.UserId, data.GuildId);
        #endregion
    }
}
