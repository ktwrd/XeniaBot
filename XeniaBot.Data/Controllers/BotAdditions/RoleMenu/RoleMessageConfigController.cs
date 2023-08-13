using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using XeniaBot.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Data.Models;

namespace XeniaBot.Data.Controllers.BotAdditions
{
    [BotController]
    public class RoleMessageConfigController : BaseConfigController<RoleMessageConfigModel>
    {
        private IMongoDatabase _db;
        public RoleMessageConfigController(IServiceProvider services)
            : base("roleMessageConfig", services)
        {
            _db = services.GetRequiredService<IMongoDatabase>();
        }

        protected async Task<IAsyncCursor<RoleMessageConfigModel>?> InternalFetch(FilterDefinition<RoleMessageConfigModel> filter)
        {
            var collection = GetCollection();
            var result = await collection.FindAsync(filter);
            return result;
        }

        public async Task<RoleMessageConfigModel?> Get(ulong messageId)
        {
            var filter = Builders<RoleMessageConfigModel>
                .Filter
                .Where(v => v.MessageId == messageId);
            var results = await InternalFetch(filter);
            return results.FirstOrDefault();
        }
        public async Task<IEnumerable<RoleMessageConfigModel>?> GetAll(
            bool all = false,
            ulong? messageId = null,
            ulong? guildId = null)
        {
            FilterDefinition<RoleMessageConfigModel>? filter = Builders<RoleMessageConfigModel>.Filter.Empty;

            if (!all)
            {
                Func<RoleMessageConfigModel, bool> matchLogic = (v) =>
                {
                    int c = 0;
                    int mc = 0;

                    mc += messageId == null ? 0 : 1;
                    mc += guildId == null ? 0 : 1;

                    c += v.MessageId == messageId ? 1 : 0;
                    c += v.GuildId == messageId ? 1 : 0;

                    return c >= mc;
                };

                filter = Builders<RoleMessageConfigModel>
                    .Filter
                    .Where(v => matchLogic(v));
            }

            var results = await InternalFetch(filter);
            return results.ToEnumerable();
        }

        public async Task Set(RoleMessageConfigModel model)
        {
            var collection = GetCollection();
            var filter = Builders<RoleMessageConfigModel>
            .Filter.Eq("MessageId", model.MessageId);

            var exists = (await collection?.CountAsync(filter)) > 0;
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
