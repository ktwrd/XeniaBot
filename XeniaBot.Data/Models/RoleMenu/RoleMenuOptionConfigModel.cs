using Discord.WebSocket;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models;

public enum CanGrantRoleResponseCode
{
    Grant,
    /// <summary>
    /// Do not grant role. Member is missing required role.
    /// </summary>
    Block_Whitelist,
    /// <summary>
    /// Do not grant role. Member has role in <see cref="RoleMenuOptionConfigModel.UserRoleBlacklist"/>
    /// </summary>
    Block_Blacklist
}
public class RoleMenuOptionConfigModel : BaseModel
{
    public static string CollectionName => "roleMenu_select_option";
    public string RoleOptionId { get; set; }
    public string RoleSelectId { get; set; }
    public long ModifiedAtTimestamp { get; set; }
    
    /// <summary>
    /// Role Id to grant this user when this is selected.
    /// </summary>
    public ulong RoleId { get; set; }
    /// <summary>
    /// List of roles the user must have to get this role. When empty, it will be ignored.
    /// </summary>
    public List<ulong> UserRoleWhitelist { get; set; }
    /// <summary>
    /// When `true`, member will need all roles in &lt;see cref="UserRoleWhitelist"/&gt;.
    ///
    /// When `false`, member will only need at least 1 role in <see cref="UserRoleWhitelist"/>.
    /// </summary>
    public bool RequireAllInWhitelist { get; set; }
    /// <summary>
    /// List of roles the user is not allowed to have to get this role. When empty, it will be ignored.
    ///
    /// When a user has more than one role that is in this list, <see cref="CanGrantRole"/> will return in <see cref="CanGrantRoleResponseCode.Block_Blacklist"/>
    /// </summary>
    public List<ulong> UserRoleBlacklist { get; set; }

    public CanGrantRoleResponseCode CanGrantRole(SocketGuildUser user)
    {
        if (UserRoleWhitelist.Count > 0)
        {
            int foundCount = 0;
            foreach (var i in UserRoleWhitelist)
            {
                foreach (var u in user.Roles)
                {
                    if (i == u.Id)
                    {
                        foundCount++;
                    }
                }
            }

            if (foundCount < 1)
                return CanGrantRoleResponseCode.Block_Whitelist;
            if (foundCount != UserRoleWhitelist.Count && RequireAllInWhitelist)
                return CanGrantRoleResponseCode.Block_Whitelist;
        }

        if (UserRoleBlacklist.Count > 0)
        {
            foreach (var i in UserRoleBlacklist)
            {
                foreach (var u in user.Roles)
                {
                    if (i == u.Id)
                        return CanGrantRoleResponseCode.Block_Blacklist;
                }
            }
        }

        return CanGrantRoleResponseCode.Grant;
    }
    
    public string OptionName { get; set; }
    public string OptionDescription { get; set; }
    public string OptionEmoji { get; set; }
}