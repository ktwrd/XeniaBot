using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models
{
    public class BanSyncInfoModel : BaseModel
    {
        public static string CollectionName => "banSyncInfo";
        public string RecordId { get; set; }
        /// <summary>
        /// Snowflake for the User that was banned
        /// </summary>
        public ulong UserId { get; set; }
        /// <summary>
        /// Username for the User that was banned
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// Discriminator for the User that was banned
        /// </summary>
        public string? UserDiscriminator { get; set; }
        /// <summary>
        /// Display Name for the User that was banned
        /// </summary>
        public string UserDisplayName { get; set; }
        public ulong GuildId { get; set; }
        public string GuildName { get; set; }
        /// <summary>
        /// Unix Epoch in UTC Seconds
        /// </summary>
        public long Timestamp { get; set; }
        /// <summary>
        /// Reason why this User was banned.
        /// </summary>
        public string Reason { get; set; }
        /// <summary>
        /// Pretend that this record doesn't exist when `true`.
        /// </summary>
        public bool Ghost { get; set; }
        public BanSyncInfoModel()
        {
            Reason = "<unknown>";
            RecordId = Guid.NewGuid().ToString();
            Ghost = false;
        }
    }
}
