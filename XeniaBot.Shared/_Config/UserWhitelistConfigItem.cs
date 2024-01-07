using System;
using System.Linq;

namespace XeniaBot.Shared;

public class UserWhitelistConfigItem
{
    /// <summary>
    /// This really isn't checked for. I don't remember why this exists in the first place, mainly for backwards compatibility with <see cref="UserWhitelistConfigItem.Enable"/>
    /// </summary>
    public bool Enable { get; set; }
    /// <summary>
    /// Users that should have access to restricted commands/functions.
    /// </summary>
    public ulong[] Users { get; set; }

    public static UserWhitelistConfigItem Default(UserWhitelistConfigItem? i = null)
    {
        i ??= new UserWhitelistConfigItem();
        i.Enable = true;
        i.Users = Array.Empty<ulong>();
        return i;
    }

    public bool Contains(ulong id) => Users.Contains(id);
}