using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using SkidBot.Core.Models;
using SkidBot.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Controllers.BotAdditions
{
    [SkidController]
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
        {// ensures we don't process system/other bot messages
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
                await GrantXp(data);
            }
        }

        public async Task GrantXp(LevelMemberModel model)
        {
            var data = await Get(model.UserId, model.GuildId);
            var amount = (ulong)_random.Next(1, 5);
            data.Xp += amount;
            await Set(data);
        }

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

        private bool FieldSearchFunction(LevelMemberModel model, ulong? user=null, ulong? guild=null)
        {
            return FieldSearchFunction(
                model,
                user,
                guild);
        }
        private bool FieldSearchFunction(LevelMemberModel model, ulong? user = null, ulong? guild = null, Func<ulong, bool>? xpFilter = null)
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
        }

        public async Task<LevelMemberModel?> Get(ulong? user=null, ulong? guild=null)
        {
            var filter = Builders<LevelMemberModel>
                .Filter
                .Where(v => FieldSearchFunction(v, user, guild, null));

            var result = await InternalFind(filter);
            var first = result.FirstOrDefault();
            return first;
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
                .Where(v => FieldSearchFunction(v, model.UserId, model.GuildId, null));
            var exists = (await Get(model.UserId, model.GuildId)) != null;

            var collection = GetCollection();
            if (exists)
            {
                await collection.ReplaceOneAsync(filter, model);
            }
            {
                await collection.InsertOneAsync(model);
            }
        }
    }
}
