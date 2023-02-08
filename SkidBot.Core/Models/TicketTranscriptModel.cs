using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Models
{
    public class TicketTranscriptModel
    {
        [Browsable(false)]
        public ObjectId _id { get; set; }
        public string Uid { get; set; }
        public string TicketUid { get; set; }
        public Discord.IMessage[] Messages { get; set; }
        public TicketTranscriptModel()
        {
            Uid = kate.shared.Helpers.GeneralHelper.GenerateUID();
            TicketUid = "";
            Messages = Array.Empty<Discord.IMessage>();
        }
    }
}
