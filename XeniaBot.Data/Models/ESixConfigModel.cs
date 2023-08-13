using System;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models;

public class ESixConfigModel : BaseModel
{
    public ulong GuildId = 0 ;
    public bool EnforceNSFWByDefault = false;
    public bool EnforceRandomByDefault = true;
    public ulong[] NSFWEnforceChannels = Array.Empty<ulong>();
}