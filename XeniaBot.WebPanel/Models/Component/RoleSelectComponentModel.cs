namespace XeniaBot.WebPanel.Models;

public class RoleSelectComponentModel : BaseFormItemModel
{
    public IEnumerable<StrippedRole> Roles { get; set; }
    public ulong? SelectedRoleId { get; set; }
    public string DisplayName { get; set; }
    public bool InputGroup { get; set; }
    public bool Required { get; set; }

    public RoleSelectComponentModel()
    {
        InputGroup = true;
        Required = false;
    }
}