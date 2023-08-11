using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeniaBot.Core.Models
{
    public class ConfigGuildTicketModel
    {
        [Browsable(false)]
        public ObjectId _id { get; set; }
        public ulong GuildId { get; set; }
        public ulong CategoryId { get; set; }
        public ulong RoleId { get; set; }
        public ulong LogChannelId { get; set; }
    }
}
