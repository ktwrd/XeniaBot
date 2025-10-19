using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models;

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