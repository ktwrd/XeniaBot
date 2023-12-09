using kate.shared.Helpers;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models;

public class RoleMenuSelectConfigModel : BaseModel
{
    public static string CollectionName => "roleMenu_select";
    public string RoleMenuId { get; set; }
    public string RoleSelectId { get; set; }
    public long ModifiedAtTimestamp { get; set; }
    public string Placeholder { get; set; }
    /// <summary>
    /// Minimum amount of options to be selected.
    /// </summary>
    public int Minimum { get; set; }
    /// <summary>
    /// Maximum amount of options to be selected.
    /// </summary>
    public int Maximum { get; set; }

    public RoleMenuSelectConfigModel()
    {
        RoleSelectId = GeneralHelper.GenerateUID();
        Maximum = 1;
    }
}