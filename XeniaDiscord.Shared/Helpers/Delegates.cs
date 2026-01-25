using System.Threading.Tasks;
using XeniaBot.Shared.Services;

namespace XeniaBot.Shared.Helpers
{
    public delegate void DiscordControllerDelegate(DiscordService service);
    public delegate Task TaskDelegate();
}
