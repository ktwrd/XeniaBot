using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Models
{
    public class ConfigBanSyncModel
    {
        [Browsable(false)]
        public ObjectId _id { get; set; }
        public ulong GuildId { get; set; }
        public ulong LogChannel { get; set; }
        public bool Enable { get; set; }
    }
}
