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
    public class TriviaSessionController : BaseController
    {
        public TriviaSessionController(IServiceProvider services)
            : base(services)
        {
            
        }

        public const string MongoCollectionName = "triviaSession";
        protected IMongoCollection<T>? GetCollection<T>()
        {
            return Program.GetMongoDatabase()?.GetCollection<T>(MongoCollectionName);
        }
        protected IMongoCollection<TriviaSessionModel>? GetCollection()
            => GetCollection<TriviaSessionModel>();

        protected async Task<IAsyncCursor<TriviaSessionModel>?> InternalFetch(FilterDefinition<TriviaSessionModel> filter)
        {
            var collection = GetCollection();
            var result = await collection.FindAsync(filter);
            return result;
        }
        public async Task<TriviaSessionModel?> Get(ulong guildId, ulong channelId)
        {
            var filter = Builders<TriviaSessionModel>
                .Filter
                .Where(v => v.GuildId == guildId && v.ChannelId == channelId);
            var res = await InternalFetch(filter);
            return res.FirstOrDefault();
        }
        public async Task<TriviaSessionModel?> Get(string sessionId)
        {
            var filter = Builders<TriviaSessionModel>
                .Filter
                .Where(v => v.SessionId == sessionId);
            var res = await InternalFetch(filter);
            return res.First();
        }

        public async Task<IEnumerable<TriviaSessionModel>> GetAll(
            bool all = false,
            ulong? guildId = null,
            ulong? channelId = null,
            int? maxQuestions = null,
            int? questionsCompleted = null,
            string? sessionId = null)
        {
            FilterDefinition<TriviaSessionModel> filter = Builders<TriviaSessionModel>.Filter.Empty;
            if (!all)
            {
                Func<TriviaSessionModel, bool> eval = (t) =>
                {
                    int c = 0;
                    int mc = 0;

                    mc += guildId == null ? 0 : 1;
                    mc += channelId == null ? 0 : 1;
                    mc += maxQuestions == null ? 0 : 1;
                    mc += questionsCompleted == null ? 0 : 1;
                    mc += sessionId == null ? 0 : 1;

                    c += guildId == t.GuildId ? 1 : 0;
                    c += channelId == t.ChannelId ? 1 : 0;
                    c += maxQuestions == t.MaxQuestions ? 1 : 0;
                    c += questionsCompleted == t.QuestionsCompleted ? 1 : 0;
                    c += sessionId == t.SessionId ? 1 : 0;

                    return c >= mc;
                };

                filter = Builders<TriviaSessionModel>.Filter.Where(v => eval(v));
            }

            var res = await InternalFetch(filter);
            return res.ToEnumerable();
        }

        public async Task<IEnumerable<TriviaSessionModel>> GetAll()
            => await GetAll(true);
        public async Task<IEnumerable<TriviaSessionModel>> GetAll(TriviaSessionModel model)
            => await GetAll(sessionId: model.SessionId);


        public async Task Set(TriviaSessionModel model)
        {
            var collection = GetCollection();
            var filter = Builders<TriviaSessionModel>
                .Filter
                .Eq("SessionId", model.SessionId);

            bool exists = (await Get(model.SessionId)) != null;
            if (exists)
            {
                await collection.ReplaceOneAsync(filter, model);
            }
            else
            {
                await collection.InsertOneAsync(model);
            }
        }
    }
}
