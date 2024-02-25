using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeniaBot.Data.Moderation.Models
{
    public class BanHistoryModel
    {
        public static string CollectionName => "mod_banRecord_history";
        [BsonElement("_id")]
        public Guid Id { get; set; }
        public ulong UserId { get; set; }
        public ulong GuildId { get; set; }
        public bool IsBanned { get; set; }
        /// <summary>
        /// When this person was banned. Seconds since UTC Epoch.
        /// </summary>
        public long Timestamp { get; set; }
        /// <summary>
        /// When not `null`, it is set to <see cref="BanRecordModel.Id"/> because it is a many-to-one relationship (many is <see cref="BanHistoryModel"/>).
        /// </summary>
        public Guid? BanRecordId { get; set; }
        public string? Reason { get; set; }

        public BanHistoryModel()
        {
            Id = Guid.NewGuid();
        }
    }
}
