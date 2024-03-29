using System;
using System.Collections.Generic;
using System.Linq;
using Discord.WebSocket;

namespace XeniaBot.WebPanel.Models;

public class StrippedRole
{
    /// <summary>
    /// Hex Color of <see cref="SocketRole.Color"/>. Default is `#00000000`
    /// </summary>
    public string HexColor { get; set; }
    /// <summary>
    /// Id of role. Cloned from <see cref="SocketRole.Id"/>
    /// </summary>
    public ulong RoleId { get; set; }
    /// <summary>
    /// When the role was created. <see cref="SocketRole.CreatedAt"/>
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>
    /// Name of the role. <see cref="SocketRole.Name"/>
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Position of the role. <see cref="SocketRole.Position"/>
    /// </summary>
    public int Position { get; set; }
    /// <summary>
    /// Can the current user grant this role?
    /// </summary>
    public bool CanAccess { get; set; }
    
    
    /// <summary>
    /// Generate a list of <see cref="StrippedRole"/> from a guild.
    /// </summary>
    /// <param name="guild"></param>
    /// <returns></returns>
    public static IEnumerable<StrippedRole> FromGuild(DiscordSocketClient client, SocketGuild guild)
    {
        var roles = guild.Roles;
        var currentUserRoles = guild.GetUser(client.CurrentUser.Id).Roles.ToList();
        var ourHighestRolePosition =
            currentUserRoles.OrderByDescending(v => v.Position).Select(v => v.Position).Concat(new int[]{int.MinValue})
                .OrderByDescending(v => v)
                .FirstOrDefault();
        var items = new List<StrippedRole>();
        foreach (var i in roles)
        {
            var d = StrippedRole.FromRole(client, i);
            d.CanAccess = d.Position < ourHighestRolePosition;
            items.Add(d);
        }

        return items;
    }

    public static StrippedRole FromRole(DiscordSocketClient client, SocketRole role)
    {
        var instance = new StrippedRole();
        instance.HexColor = role.Color.ToString() ?? "#00000000";
        instance.RoleId = role.Id;
        instance.CreatedAt = role.CreatedAt;
        instance.Name = role.Name;
        instance.Position = role.Position;
        return instance;
    }
}