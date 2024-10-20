﻿using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDB.Driver.Core.Bindings;
using XeniaBot.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using System.Threading.Tasks;
using XeniaBot.Data;
using XeniaBot.Data.Models;
using XeniaBot.DiscordCache.Models;
using XeniaBot.Shared.Services;

namespace XeniaBot.Core.Services.BotAdditions
{
    public class InternalTicketDetails
    {
        public TicketModel Ticket;
        public ConfigGuildTicketModel Config;
        public SocketGuild Guild;
        public SocketRole Role;
        public SocketTextChannel LogChannel;
        public SocketTextChannel TicketChannel;
        public SocketGuildUser ClosingUser;
    }
    [XeniaController]
    public class TicketService : BaseService
    {
        private readonly DiscordSocketClient _client;
        private readonly DiscordService _discord;
        public TicketService(IServiceProvider services)
            : base(services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
            _discord = services.GetRequiredService<DiscordService>();
        }
        public override Task InitializeAsync() => Task.CompletedTask;

        public async Task<TicketModel> CreateTicket(ulong guildId)
        {
            var config = await GetGuildConfig(guildId);
            if (config == null)
                throw new TicketException("Guild not setup (config is null)");

            // Fetch guild and category, throw errors if they are null
            var guild = _client.GetGuild(guildId);
            if (guild == null)
                throw new TicketException($"Guild not found ({guildId})");

            var role = guild.GetRole(config.RoleId);
            if (role == null)
                throw new TicketException($"Ticket Manager Role not found ({config.RoleId})");

            var category = guild.GetCategoryChannel(config.CategoryId);
            if (category == null)
                throw new TicketException($"Category not found ({config.CategoryId})");

            var ticketDetails = new TicketModel()
            {
                GuildId = guildId,
                Status = TicketStatus.Open,
                CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            RestTextChannel? textChannel = null;

            // Attempt to create channel, then move it to category
            try
            {
                textChannel = await guild.CreateTextChannelAsync($"ticket-{ticketDetails.Uid}");
                await textChannel.ModifyAsync(prop => prop.CategoryId = category.Id);
                ticketDetails.ChannelId = textChannel.Id;
                var ignoreOverwrite = new OverwritePermissions();
                ignoreOverwrite.Modify(viewChannel: PermValue.Deny);
                await textChannel.AddPermissionOverwriteAsync(guild.EveryoneRole, ignoreOverwrite);
                await textChannel.AddPermissionOverwriteAsync(role, GetOverwritePermissions());
            }
            catch (Exception ex)
            {
                throw new TicketException($"Failed to create ticket channel. {ex.Message}", ex);
            }

            if (textChannel == null)
                throw new TicketException("Created Text Channel is null");

            await Set(ticketDetails);

            return ticketDetails;
        }
        public async Task UserAccessGrant(ulong channelId, ulong userId)
        {
            var ticket = await Get(channelId);
            if (ticket == null)
                throw new TicketException("Ticket Details not found");

            if (ticket.Users.Contains(userId))
                throw new TicketException("User already exists in ticket");

            var guild = _client.GetGuild(ticket.GuildId);
            if (guild == null)
                throw new TicketException($"Guild not found ({ticket.GuildId})");

            var user = guild.GetUser(userId);
            if (user == null)
                throw new TicketException($"User not found ({userId})");

            var channel = guild.GetTextChannel(ticket.ChannelId);
            if (channel == null)
                throw new TicketException($"Ticket Channel not found ({ticket.ChannelId})");

            // Modify channel permission overwrites
            try
            {
                await channel.AddPermissionOverwriteAsync(user, GetOverwritePermissions());
            }
            catch (Exception ex)
            {
                throw new TicketException($"Failed to modify channel permissions. {ex.Message}", ex);
            }

            // Add user to model then save.
            ticket.Users = ticket.Users.Concat(new ulong[]
            {
                userId
            }).ToArray();

            await Set(ticket);
        }
        public async Task UserAccessRevoke(ulong channelId, ulong userId)
        {
            var ticket = await Get(channelId);
            if (ticket == null)
                throw new TicketException("Ticket Details not found");

            if (!ticket.Users.Contains(userId))
                throw new TicketException("User not included in ticket");

            var guild = _client.GetGuild(ticket.GuildId);
            if (guild == null)
                throw new TicketException($"Guild not found ({ticket.GuildId})");

            var user = guild.GetUser(userId);
            if (user == null)
                throw new TicketException($"User not found ({userId})");

            var channel = guild.GetTextChannel(ticket.ChannelId);
            if (channel == null)
                throw new TicketException($"Ticket Channel not found ({ticket.ChannelId})");

            // Remove user override
            try
            {
                await channel.RemovePermissionOverwriteAsync(user);
            }
            catch (Exception ex)
            {
                throw new TicketException($"Failed to modify channel permissions. {ex.Message}", ex);
            }

            // Remove userId from model then save
            ticket.Users = ticket.Users.Where(v => v != userId).ToArray();
            await Set(ticket);
        }
        private async Task<InternalTicketDetails> GetTicketDetails(ulong ticketChannelId)
        {
            var ticket = await Get(ticketChannelId);
            if (ticket == null)
                throw new TicketException("Ticket Details not found");

            var config = await GetGuildConfig(ticket.GuildId);
            if (config == null)
                throw new TicketException("Guild not setup");

            var guild = _client.GetGuild(ticket.GuildId);
            if (guild == null)
                throw new TicketException($"Guild not found ({ticket.GuildId})");

            var role = guild.GetRole(config.RoleId);
            if (role == null)
                throw new TicketException($"Ticket Manager Role not found ({config.RoleId})");

            var logChannel = guild.GetTextChannel(config.LogChannelId);
            if (logChannel == null)
                throw new TicketException($"Ticket Log Channel not found ({config.LogChannelId})");

            var ticketChannel = guild.GetTextChannel(ticket.ChannelId);
            if (ticketChannel == null)
                throw new TicketException($"Ticket Channel not found ({ticket.ChannelId})");

            return new InternalTicketDetails()
            {
                Ticket = ticket,
                Config = config,
                Guild = guild,
                Role = role,
                LogChannel = logChannel,
                TicketChannel = ticketChannel
            };
        }
        private async Task<InternalTicketDetails> GetTicketDetails(ulong ticketChannelId, ulong closingUserId)
        {
            var details = await GetTicketDetails(ticketChannelId);
            var closingUser = details.Guild.GetUser(closingUserId);
            if (closingUser == null)
                throw new TicketException($"Could not find user who closed this ticket ({closingUserId})");

            details.ClosingUser = closingUser;

            return details;
        }
        /// <summary>
        /// Mark a ticket as closed.
        ///
        /// This will take a backup of the channel content, mark the ticket as closed and who did it. After all of those things are done, it will delete the channel.
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="status"></param>
        /// <param name="closingUserId"></param>
        /// <returns></returns>
        /// <exception cref="TicketException"></exception>
        public async Task<TicketTranscriptModel> CloseTicket(ulong channelId, TicketStatus status, ulong closingUserId)
        {
            var details = await GetTicketDetails(channelId, closingUserId);

            details.Ticket.ClosedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            details.Ticket.Status = status;
            details.Ticket.ClosedByUserId = closingUserId;
            await Set(details.Ticket);

            details.Ticket = await Get(details.Ticket.ChannelId);
            if (details.Ticket == null)
                throw new TicketException("Ticket Details not found");

            var transcript = await GenerateTranscript(details.Ticket);
            if (transcript == null)
                throw new TicketException("Failed to generate transcript");

            var channelBackup = new List<CacheMessageModel>();
            try
            {
                channelBackup = await GenerateChannelBackup(details.Ticket);
            }
            catch (Exception ex)
            {
                throw new TicketException("Failed to generate channel backup", ex);
            }

            var channelBackupSer = JsonSerializer.Serialize(
                channelBackup, new JsonSerializerOptions()
                {
                    IncludeFields = true,
                    WriteIndented = true,
                    ReferenceHandler = ReferenceHandler.Preserve
                });

            var attachment = string.Join("\n", transcript.ToString());
            var embed = new EmbedBuilder()
            {
                Title = $"Ticket Marked as {status}",
                Description = "Users Included\n"
                    + string.Join("\n", details.Ticket.Users.Select(v => $"<@{v}>"))
                    + "\n\n"
                    + string.Join("\n", details.Role.Members.Select(v => $"<@{v.Id}>")),
            };
            embed.WithFooter($"Closed by {details.ClosingUser.Username}#{details.ClosingUser.Discriminator} {details.ClosingUser.Id}", details.ClosingUser.GetAvatarUrl());

            var attachmentList = new List<FileAttachment>();
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(attachment)))
            {
                attachmentList.Add(new FileAttachment(ms, "transcript.txt"));
            }

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(channelBackupSer)))
            {
                attachmentList.Add(new FileAttachment(ms, "channelContent.json"));
            }
            await details.LogChannel.SendFilesAsync(attachmentList, "", embed: embed.Build());

            await details.TicketChannel.DeleteAsync();

            return transcript;
        }
        protected OverwritePermissions GetOverwritePermissions()
        {
            var perms = new OverwritePermissions();
            perms.Modify(
                viewChannel: PermValue.Allow,
                readMessageHistory: PermValue.Allow,
                sendMessages: PermValue.Allow);
            return perms;
        }

        #region MongoDB TicketTranscript Boilerplate
        public const string MongoTranscriptCollectionName = "ticketTranscript";
        protected static IMongoCollection<T>? GetTranscriptCollection<T>()
        {
            return Program.Core.GetDatabase()?.GetCollection<T>(MongoTranscriptCollectionName);
        }
        protected static IMongoCollection<TicketTranscriptModel>? GetTranscriptCollection()
            => GetTranscriptCollection<TicketTranscriptModel>();
        public async Task<TicketTranscriptModel?> GetTranscript(string transcriptUid)
        {
            var collection = GetTranscriptCollection();
            var filter = Builders<TicketTranscriptModel>
                .Filter
                .Eq("Uid", transcriptUid);

            var items = await collection.FindAsync(filter);

            return items.FirstOrDefault();
        }
        public async Task<TicketTranscriptModel?> GenerateTranscript(TicketModel ticket)
        {
            var guild = _client.GetGuild(ticket.GuildId);
            if (guild == null)
                throw new Exception($"Failed to fetch guild {ticket.GuildId}");
            var channel = guild.GetTextChannel(ticket.ChannelId);
            if (channel == null)
                throw new Exception($"Failed to fetch ticket channel {ticket.ChannelId}");

            // Fetch all messages in channel
            var messages = await channel.GetMessagesAsync(int.MaxValue).FlattenAsync();

            var model = new TicketTranscriptModel()
            {
                TicketUid = ticket.Uid,
            };
            var _tk = await Get(ticket.ChannelId);
            if (_tk != null)
            {
                _tk.TranscriptUid = model.Uid;
                await Set(_tk);
            }

            model.Messages = messages.Select(v => TicketTranscriptMessage.FromMessage(v)).ToArray();

            var collection = GetTranscriptCollection();
            await collection.InsertOneAsync(model);

            return model;
        }

        /// <summary>
        /// Generate a backup of a ticket channel. Converts messages into <see cref="CacheMessageModel"/>
        /// </summary>
        /// <returns>List of messages in the specified ticket channel.</returns>
        /// <exception cref="InnerTicketException">Thrown when logic inside of this function is fucked.</exception>
        public async Task<List<CacheMessageModel>> GenerateChannelBackup(TicketModel ticket)
        {
            var guild = _client.GetGuild(ticket.GuildId);
            if (guild == null)
                throw new InnerTicketException($"Guild {ticket.GuildId} not found.", ticket);

            var channel = guild.GetTextChannel(ticket.ChannelId);
            if (channel == null)
                throw new InnerTicketException($"Guild {ticket.ChannelId} not found.", ticket);

            var messages = await channel.GetMessagesAsync(int.MaxValue).FlattenAsync();
            var msgArr = messages.ToArray();

            var result = new List<CacheMessageModel>();
            for (int i = 0; i < msgArr.Length; i++)
            {
                var msg = msgArr[i];
                try
                {
                    var converted = CacheMessageModel.FromExisting(msg);
                    if (converted != null)
                        result.Add(converted);
                }
                catch (Exception ex)
                {
                    throw new InnerTicketException(
                        $"Failed to convert message at index {i} (ID: {msg.Id}) to CacheMessageModel", ticket, ex);
                }
            }

            return result;
        }

        #endregion

        #region MongoDB TicketModel Boilerplate
        public const string MongoTicketCollectionName = "ticketDetails";
        protected static IMongoCollection<T>? GetTicketCollection<T>()
        {
            return Program.Core.GetDatabase()?.GetCollection<T>(MongoTicketCollectionName);
        }
        protected static IMongoCollection<TicketModel>? GetTicketCollection()
            => GetTicketCollection<TicketModel>();

        public async Task<TicketModel?> Get(ulong channelId)
        {
            var collection = GetTicketCollection();
            var filter = Builders<TicketModel>
                .Filter
                .Eq("ChannelId", channelId);

            var items = await collection.FindAsync(filter);

            return items.FirstOrDefault();
        }
        public async Task Set(TicketModel model)
        {
            var collection = GetTicketCollection();
            var filter = Builders<TicketModel>
                .Filter
                .Eq("ChannelId", model.ChannelId);

            if (await (await collection.FindAsync(filter)).AnyAsync())
                await collection.FindOneAndReplaceAsync(filter, model);
            else
                await collection.InsertOneAsync(model);
        }
        #endregion

        #region MongoDB Config Boilerplate
        public const string MongoConfigCollectionName = "ticketGuildConfig";
        protected static IMongoCollection<T>? GetConfigCollection<T>()
        {
            return Program.Core.GetDatabase()?.GetCollection<T>(MongoConfigCollectionName);
        }
        protected static IMongoCollection<ConfigGuildTicketModel>? GetConfigCollection()
            => GetConfigCollection<ConfigGuildTicketModel>();
        public async Task<ConfigGuildTicketModel?> GetGuildConfig(ulong guildId)
        {
            var collection = GetConfigCollection();
            var filter = Builders<ConfigGuildTicketModel>
                .Filter
                .Eq("GuildId", guildId);

            var item = await collection.FindAsync(filter);

            return item.FirstOrDefault();
        }

        public async Task SetGuildConfig(ConfigGuildTicketModel model)
        {
            var collection = GetConfigCollection();
            var filter = Builders<ConfigGuildTicketModel>
                .Filter
                .Eq("GuildId", model.GuildId);

            if (await (await collection.FindAsync(filter)).AnyAsync())
                await collection.FindOneAndReplaceAsync(filter, model);
            else
                await collection.InsertOneAsync(model);
        }
        public async Task DeleteGuildConfig(ulong guildId)
        {
            var collection = GetConfigCollection();
            var filter = Builders<ConfigGuildTicketModel>
                .Filter
                .Eq("GuildId", guildId);

            await collection.DeleteManyAsync(filter);
        }
        public Task DeleteGuildConfig(ConfigGuildTicketModel model)
            => DeleteGuildConfig(model.GuildId);
        #endregion
    }
}
