using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Models
{
    public class RoleMessageConfigModel
    {
        [Browsable(false)]
        public ObjectId _id { get; set; }
        public uint GuildId { get; set; }
        public uint MessageId { get; set; }
        /// <summary>
        /// Key: <see cref="Discord.IEmote.Name"/>
        /// Value: <see cref="RoleConfigModel.Uid"/>
        /// </summary>
        public Dictionary<string, string> ReactionRoleMap { get; set; }
        public RoleMessageConfigModel()
        {
            ReactionRoleMap = new Dictionary<string, string>();
        }
    }
}
