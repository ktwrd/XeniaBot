using Discord;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Models
{
    public class CounterGuildModel
    {
        [Browsable(false)]
        public ObjectId _id { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong Count { get; set; }
        public CounterGuildModel()
            : this(null, null)
        {
        }
        public CounterGuildModel(IChannel channel = null, IGuild guild = null)
        {
            ChannelId = channel?.Id ?? 0;
            GuildId = guild?.Id ?? 0;
            Count = 0;
        }
    }
}
