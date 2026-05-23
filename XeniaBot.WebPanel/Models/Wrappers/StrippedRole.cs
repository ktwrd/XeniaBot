using CSharpFunctionalExtensions;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using Wacton.Unicolour;
using XeniaDiscord.Common;
using XeniaDiscord.Data.Models.Snapshot;

namespace XeniaBot.WebPanel.Models;

public class StrippedRole : IStrippedRole
{
    /// <summary>
    /// Hex Color of <see cref="SocketRole.Color"/>. Default is <c>#000000</c>
    /// </summary>
    public string HexColor { get; set; } = DefaultHexColor;

    public const string DefaultHexColor = "#000000";

    public Unicolour RoleUnicolour
        => new(HexColor);

    private static readonly Unicolour Dark = new("404046");
    private static readonly Unicolour Light = new("e8e8ff");
    public bool ShouldUseLightText()
    {
        var color = RoleUnicolour;
        if (color.HasConversionError()) return true;
        var inGamut = color.MapToRgbGamut(GamutMap.RgbClipping);
        return inGamut.Contrast(Light) > inGamut.Contrast(Dark);
    }

    /// <summary>
    /// Role ID. Cloned from <see cref="SocketRole.Id"/>
    /// </summary>
    public ulong Id { get; set; }
    
    /// <summary>
    /// When the role was created. <see cref="SocketRole.CreatedAt"/>
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Name of the role. <see cref="SocketRole.Name"/>
    /// </summary>
    public string Name { get; set; } = "unknown";
    
    /// <summary>
    /// Position of the role. <see cref="SocketRole.Position"/>
    /// </summary>
    public int Position { get; set; }
    
    /// <summary>
    /// Can the current user grant this role?
    /// </summary>
    public bool CanAccess { get; set; }

    public Maybe<GuildPermissions> Permissions { get; set; } = Maybe.None;

    /// <summary>
    /// Generate a list of <see cref="StrippedRole"/> from a guild.
    /// </summary>
    public static IEnumerable<StrippedRole> FromGuild(DiscordSocketClient client, SocketGuild guild)
    {
        var roles = guild.Roles;
        var currentUserRoles = guild.GetUser(client.CurrentUser.Id).Roles.ToList();
        var ourHighestRolePosition = currentUserRoles
            .OrderByDescending(v => v.Position)
            .Select(v => v.Position)
            .Concat([int.MinValue])
            .OrderByDescending(v => v)
            .FirstOrDefault();
        
        var items = new List<StrippedRole>();
        foreach (var i in roles)
        {
            var d = FromRole(i);
            d.CanAccess = d.Position < ourHighestRolePosition;
            items.Add(d);
        }

        return items;
    }

    public static StrippedRole FromRole(SocketRole role)
    {
        var color = role.Colors.PrimaryColor.ToString() ?? DefaultHexColor;
        return new StrippedRole
        {
            HexColor = color,
            Id = role.Id,
            CreatedAt = role.CreatedAt,
            Name = role.Name,
            Position = role.Position,
            Permissions = role.Permissions
        };
    }

    public static StrippedRole FromRole(GuildRoleSnapshotModel model)
    {
        var roleId = model.GetRoleId();
        var color = model.RoleColors == null
            ? DefaultHexColor
            : model.RoleColors.GetPrimaryColor().ToString();
        return new StrippedRole
        {
            Id = roleId,
            CreatedAt = model.CreatedAt,
            Name = model.Name ?? model.RoleId,
            Position = model.Position,
            Permissions = model.ParsePermissions(),
            HexColor = color
        };
    }
}

public interface IStrippedRole
{
    string HexColor { get; }
    ulong Id { get; }
    DateTimeOffset CreatedAt { get; }
    string Name { get; }
    int Position { get; }
    bool CanAccess { get; }
    Maybe<GuildPermissions> Permissions { get; }
}