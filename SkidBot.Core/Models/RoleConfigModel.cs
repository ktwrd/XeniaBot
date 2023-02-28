using kate.shared.Helpers;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Models
{
    public class RoleConfigModel
    {
        [Browsable(false)]
        public ObjectId _id { get; set; }
        public string Uid { get; set; }
        public ulong GuildId { get; set; }
        public ulong RoleId { get; set; }
        /// <summary>
        /// User must have RequiredRoleId in order to get this role. 0 for ignore
        /// </summary>
        public ulong RequiredRoleId { get; set; }
        /// <summary>
        /// If user has BlacklistRoleId then they cannot get this role. 0 for ignore
        /// </summary>
        public ulong BlacklistRoleId { get; set; }
        public string Name { get; set; }
        public string Group { get; set; }
        public RoleConfigModel()
        {
            Uid = GeneralHelper.GenerateUID();
            Name = "";
            Group = "<none>";
        }
    }
}
