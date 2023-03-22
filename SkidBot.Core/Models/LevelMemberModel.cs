using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Models
{
    public class LevelMemberModel : BaseModel
    {
        public ulong UserId { get; set; }
        public ulong GuildId { get; set; }
        public ulong Xp { get; set; }

        public LevelMemberModel()
        {
            UserId = 0;
            GuildId = 0;
            Xp = 0;
        }
    }
}
