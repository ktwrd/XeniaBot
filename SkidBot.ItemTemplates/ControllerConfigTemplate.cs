using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkidBot.Shared;

namespace $rootnamespace$
{
    [SkidControllerAttribute]
public class $safeitemrootname$ : BaseController
    {
        private IMongoDatabase _db;
    public $safeitemrootname$(IServiceProvider services)
            : base (services)
        {
            _db = services.GetRequiredService<IMongoDatabase>();

        }

        public const string MongoCollectionName = "";
        protected static IMongoCollection<T>? GetCollection()
        {
            return _db.GetCollection<T>(MongoCollectionName);
        }
    }
}
