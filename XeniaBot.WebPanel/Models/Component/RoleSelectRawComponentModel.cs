namespace XeniaBot.WebPanel.Models;

public class RoleSelectRawComponentModel
{
    public IEnumerable<StrippedRole> Roles { get; set; }
    public ulong? SelectedRoleId { get; set; }
}