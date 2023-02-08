using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Models
{
    public class TicketModel
    {
        [Browsable(false)]
        public ObjectId _id { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong[] Users { get; set; }
        public long CreatedTimestamp { get; set; }
        public long ClosedTimestamp { get; set; }
        public TicketStatus Status { get; set; }
        public string TranscriptUid { get; set; }
        public string Uid { get; set; }
        public TicketModel()
        {
            GuildId = 0;
            ChannelId = 0;
            Users = Array.Empty<ulong>();
            CreatedTimestamp = 0;
            ClosedTimestamp = 0;
            Status = TicketStatus.Unknown;
            TranscriptUid = kate.shared.Helpers.GeneralHelper.GenerateUID();
            Uid = kate.shared.Helpers.GeneralHelper.GenerateUID();
        }
    }
    public enum TicketStatus
    {
        Unknown = -1,
        Open,
        Resolved,
        Rejected
    }
}
