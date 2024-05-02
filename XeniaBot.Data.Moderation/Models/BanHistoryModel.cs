using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Moderation.Models
{
    public class BanHistoryModel : BaseModelGuid
    {
        public static string CollectionName => "mod_banRecord_history";
        /// <summary>
        /// <para><b>Stored as ulong</b></para>
        ///
        /// <inheritdoc cref="GetUserId()"/>
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// User that was banned or unbanned.
        /// </summary>
        public ulong GetUserId()
        {
            return ulong.Parse(UserId);
        }
        /// <summary>
        /// <para><b>Stored as ulong</b></para>
        ///
        /// <inheritdoc cref="GetActionedByUserId()"/>
        /// </summary>
        public string? ActionedByUserId { get; set; }

        /// <summary>
        /// <para>User Id that banned/unbanned the User. Null when not known</para>
        /// </summary>
        public ulong? GetActionedByUserId()
        {
            return ActionedByUserId == null ? null : ulong.Parse(ActionedByUserId);
        }
        /// <summary>
        /// <para><b>Stored as ulong</b></para>
        ///
        /// <inheritdoc cref="GetGuildId()"/>
        /// </summary>
        public string GuildId { get; set; }

        /// <summary>
        /// Guild Id this Ban History item is for.
        /// </summary>
        public ulong GetGuildId()
        {
            return ulong.Parse(GuildId);
        }
        /// <summary>
        /// Is the User banned?
        /// </summary>
        public bool IsBanned { get; set; }
        /// <summary>
        /// <para>Unix Timestamp (UTC, <b>Seconds</b>)</para>
        /// 
        /// <para>When this person was banned.</para>
        /// </summary>
        public long Timestamp { get; set; }
        /// <summary>
        /// When not `null`, it is set to <see cref="BanRecordModel.Id"/> because it is a many-to-one relationship (many is <see cref="BanHistoryModel"/>).
        /// </summary>
        public string? BanRecordId { get; set; }
        /// <summary>
        /// Reason why this User was banned.
        /// </summary>
        public string? Reason { get; set; }

        public BanHistoryModel()
            : base()
        {
            UserId = "0";
            GuildId = "0";
        }
    }
}
