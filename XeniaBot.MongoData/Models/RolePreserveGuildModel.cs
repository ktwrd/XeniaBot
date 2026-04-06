using System;
using XeniaBot.Shared.Models;

namespace XeniaBot.MongoData.Models;

[Obsolete]
public class RolePreserveGuildModel : BaseModel
{
    public static string CollectionName => "rolePreserve_guild";
    public ulong GuildId { get; set; }
    public bool Enable { get; set; }

    public RolePreserveGuildModel() : base()
    {
        Enable = false;
    }
}