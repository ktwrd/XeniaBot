using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Shared.Services;

namespace XeniaBot.Shared.Helpers
{
    public delegate void DiscordControllerDelegate(DiscordService service);
    public delegate Task TaskDelegate();
}
