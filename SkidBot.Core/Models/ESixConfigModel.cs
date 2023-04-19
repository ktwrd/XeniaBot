using System;

namespace SkidBot.Core.Models;

public class ESixConfigModel : BaseModel
{
    public ulong GuildId = 0 ;
    public bool EnforceNSFWByDefault = false;
    public bool EnforceRandomByDefault = true;
    public ulong[] NSFWEnforceChannels = Array.Empty<ulong>();
}