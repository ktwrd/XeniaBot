using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Shared.Services;

namespace XeniaBot.Shared.Helpers
{
    public delegate void DiscordControllerDelegate(DiscordService service);
    public delegate Task TaskDelegate();

    public delegate void LockStateItemDelegate<T, TH>(
        [DisallowNull] T key,
        [DisallowNull]TH value);
}
