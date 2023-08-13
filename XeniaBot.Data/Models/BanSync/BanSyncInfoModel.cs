using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeniaBot.Data.Models
{
    public class BanSyncInfoModel
    {
        [Browsable(false)]
        public ObjectId _id { get; set; }
        public ulong UserId { get; set; }
        public string UserName { get; set; }
        public string UserDiscriminator { get; set; }
        public ulong GuildId { get; set; }
        public string GuildName { get; set; }
        /// <summary>
        /// Unix Epoch in UTC Seconds
        /// </summary>
        public double Timestamp { get; set; }
        public string Reason { get; set; }
        public BanSyncInfoModel()
        {
            Reason = "<unknown>";
        }
    }
}
