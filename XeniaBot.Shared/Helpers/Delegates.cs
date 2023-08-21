using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Shared.Controllers;

namespace XeniaBot.Shared.Helpers
{
    public delegate void DiscordControllerDelegate(DiscordController controller);
    public delegate Task TaskDelegate();
}
