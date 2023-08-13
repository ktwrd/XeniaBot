using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeniaBot.Data.Models
{
    public class ConfigBanSyncModel
    {
        [Browsable(false)]
        public ObjectId _id { get; set; }
        public ulong GuildId { get; set; }
        public ulong LogChannel { get; set; }
        public bool Enable { get; set; }
        public BanSyncGuildState State { get; set; }
        public string Reason { get; set; }
        public ConfigBanSyncModel()
        {
            GuildId = 0;
            LogChannel = 0;
            Enable = false;
            State = BanSyncGuildState.Unknown;
            Reason = "";
        }
    }
    public enum BanSyncGuildState
    {
        Unknown = -1,
        PendingRequest,
        RequestDenied,
        Blacklisted,
        Active
    }
}
