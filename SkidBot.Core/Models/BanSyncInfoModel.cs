using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Models
{
    public class BanSyncInfoModel
    {
        [Browsable(false)]
        public ObjectId _id { get; set; }
        public ulong UserId { get; set; }
        public ulong GuildId { get; set; }
        public ulong BannerUserId { get; set; }
        public double Timestamp { get; set; }
        public string Reason { get; set; }
        public BanSyncInfoModel()
        {
            Reason = "<unknown>";
        }
    }
}
