using System;
using XeniaBot.Shared.Models;

namespace XeniaBot.MongoData.Models;

public class ESixConfigModel : BaseModel
{
    public ulong GuildId { get; set; } = 0;
    public bool EnforceNSFWByDefault { get; set; } = false;
    public bool EnforceRandomByDefault { get; set; } = true;
    public ulong[] NSFWEnforceChannels { get; set; } = Array.Empty<ulong>();
}