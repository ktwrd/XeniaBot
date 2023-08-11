using XeniaBot.Core.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeniaBot.Core.Helpers
{
    public delegate void DiscordControllerDelegate(DiscordController controller);
    public delegate Task TaskDelegate();
}
