using Discord;
using Discord.Commands;
using Discord.WebSocket;
using kate.shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using XeniaBot.Core.Helpers;
using XeniaBot.Core.Models;
using XeniaBot.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeniaBot.Core.Controllers.BotAdditions
{
    [BotController]
    public class LevelSystemController : BaseController
    {
        private IMongoDatabase _db;
        private DiscordSocketClient _client;
        private Random _random;
        public LevelSystemController(IServiceProvider services)
            : base(services)
        {
            _db = services.GetRequiredService<IMongoDatabase>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _random = new Random();
            _client.MessageReceived += _client_MessageReceived;
        }

        private async Task _client_MessageReceived(SocketMessage rawMessage)
        {
            // Ignore messages from bots & webhooks
            if (rawMessage.Author.IsBot || rawMessage.Author.IsWebhook)
                return;
            // ensures we don't process system/other bot messages
            if (!(rawMessage is SocketUserMessage message))
            {
                return;
            }
            var context = new SocketCommandContext(_client, message);
            var data = await Get(message.Author.Id, context.Guild.Id);
            if (data == null)
                data = new LevelMemberModel()
                {
                    UserId = message.Author.Id,
                    GuildId = context.Guild.Id
                };
            await Set(data);

            var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var previousMessageDiff = currentTimestamp - data.LastMessageTimestamp;
            if (previousMessageDiff >= 8000)
            {
                var result = await GrantXp(data, message);
                if (result.DidLevelUp)
                {
                    await message.ReplyAsync($"Leveled up to {result.Metadata.UserLevel}!");
                }
            }
        }

        public class GrantXpResult
        {
            /// <summary>
            /// Did this cause the user to level up
            /// </summary>
            public bool DidLevelUp { get; init; }
            /// <summary>
            /// New XP Metadata
            /// </summary>
            public ExperienceMetadata Metadata { get; init; }
        }
        /// <summary>
        /// Grant user 4 to 16 xp.
        /// </summary>
        /// <param name="model">User XP Data</param>
        /// <param name="message">Message that triggered this event</param>
        /// <returns>Result information. See <see cref="GrantXpResult"/></returns>
        public async Task<GrantXpResult> GrantXp(LevelMemberModel model, SocketUserMessage message)
        {
            var data = await Get(model.UserId, model.GuildId);
            var amount = (ulong)_random.Next(4, 16);

            // Generate previous and current metadata
            var metadataPrevious = LevelSystemHelper.Generate(data);
            data.Xp += amount;
            var metadata = LevelSystemHelper.Generate(data);

            // Set previous Ids
            data.LastMessageChannelId = message.Channel.Id;
            data.LastMessageId = message.Id;

            bool levelUp = metadataPrevious.UserLevel != metadata.UserLevel;
            if (levelUp)
            {
                OnUserLevelUp(model, metadataPrevious, metadata);
            }

            await Set(data);
            return new GrantXpResult()
            {
                DidLevelUp = levelUp,
                Metadata = metadata
            };
        }
        protected void OnUserLevelUp(LevelMemberModel model, ExperienceMetadata previous, ExperienceMetadata current)
        {
            if (UserLevelUp != null)
            {
                UserLevelUp?.Invoke(model, previous, current);
            }
        }
        public event ExperienceComparisonDelegate UserLevelUp;

        #region MongoDB
        public const string MongoCollectionName = "levelSystem";
        protected IMongoCollection<LevelMemberModel> GetCollection()
        {
            return _db.GetCollection<LevelMemberModel>(MongoCollectionName);
        }
        protected async Task<IAsyncCursor<LevelMemberModel>?> InternalFind(FilterDefinition<LevelMemberModel> filter)
        {
            var collection = GetCollection();
            var result = await collection.FindAsync(filter);
            return result;
        }

        public async Task<LevelMemberModel?> Get(ulong? user=null, ulong? guild=null)
        {
            var filter = Builders<LevelMemberModel>
                .Filter
                .Where(v => v.UserId == user && v.GuildId == guild);

            var result = await InternalFind(filter);
            var first = result.FirstOrDefault();
            return first;
        }

        public async Task<LevelMemberModel[]?> GetGuild(ulong guildId)
        {
            var collection = GetCollection();
            var filter = Builders<LevelMemberModel>
                .Filter
                .Where(v => v.GuildId == guildId);

            var result = await collection.FindAsync(filter);
            var item = await result.ToListAsync();
            return item.ToArray();
        }
        /// <summary>
        /// Delete many objects from the database
        /// </summary>
        /// <returns>Amount of items deleted</returns>
        public async Task<long> Delete(ulong? user=null, ulong? guild=null, Func<ulong, bool>? xpFilter=null)
        {
            Func<LevelMemberModel, bool> filterFunction = (model) =>
            {
                int found = 0;
                int required = 0;

                required += user == null ? 0 : 1;
                required += guild == null ? 0 : 1;
                required += xpFilter == null ? 0 : 1;

                found += user == model.UserId ? 1 : 0;
                found += guild == model.GuildId ? 1 : 0;
                if (xpFilter != null)
                {
                    found += xpFilter(model.Xp) ? 1 : 0;
                }

                return found >= required;
            };

            var filter = Builders<LevelMemberModel>
                .Filter
                .Where(v => filterFunction(v));

            var collection = GetCollection();
            var count = await collection.CountDocumentsAsync(filter);
            if (count < 1)
                return count;

            await collection.DeleteManyAsync(filter);
            return count;
        }

        public async Task Set(LevelMemberModel model)
        {
            var filter = Builders<LevelMemberModel>
                .Filter
                .Where(v => v.UserId == model.UserId && v.GuildId == model.GuildId);
            var exists = (await Get(model.UserId, model.GuildId)) != null;

            var collection = GetCollection();
            // Replace if exists, if not then we just insert
            if (exists)
            {
                await collection.ReplaceOneAsync(filter, model);
            }
            else
            {
                await collection.InsertOneAsync(model);
            }
        }
        #endregion
    }
}
