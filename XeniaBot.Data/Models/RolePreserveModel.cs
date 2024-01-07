using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models;

public class RolePreserveModel : BaseModel
{
    public static string CollectionName => "rolePreserve";

    public RolePreserveModel()
        : base()
    {
        Roles = new List<ulong>();
    }
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public List<ulong> Roles { get; set; }
}