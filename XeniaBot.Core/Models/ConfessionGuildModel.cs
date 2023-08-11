using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeniaBot.Core.Models
{
    public class ConfessionGuildModel
    {
        [Browsable(false)]
        public ObjectId _id { get; set; }
        public ulong ChannelId { get; set; }
        public ulong GuildId { get; set; }
        public ulong ModalMessageId { get; set; }
        public ulong ModalChannelId { get; set; }
    }
}
