using Discord;
using MongoDB.Driver;
using System.Drawing;
using System.Xml.Linq;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.DiscordCache.Models;

public class CacheRole : ICacheRole
{
    public ulong? RoleId { get; set; }
    public ulong GuildId { get; set; }
    public string Color { get; set; }
    public bool IsHoisted { get; set; }
    public bool IsManaged { get; set; }
    public bool IsMentionable { get; set; }
    public string Name { get; set; }
    public string? Icon { get; set; }
    public CacheEmote? Emoji { get; set; }
    public CacheGuildPermissions Permissions { get; set; }
    public int Position { get; set; }
    public RoleTags? Tags { get; set; }

    public CacheRole()
    {
        RoleId = null;
        GuildId = 0;
        Color = "000000";
        IsHoisted = false;
        IsManaged = false;
        IsMentionable = true;
        Name = "";
        Icon = null;
        Emoji = null;
        Permissions = CacheGuildPermissions.None;
        Position = 0;
        Tags = null;
    }

    public CacheRole Update(IRole role)
    {
        this.UpdateValues(role);
        return this;
    }

    public static CacheRole? FromExisting(IRole? role)
    {
        if (role == null)
            return null;
        var instance = new CacheRole();
        return instance.Update(role);
    }
}

public interface ICacheRole
{
    public ulong? RoleId { get; set; }
    public ulong GuildId { get; set; }
    public string Color { get; set; }
    public bool IsHoisted { get; set; }
    public bool IsManaged { get; set; }
    public bool IsMentionable { get; set; }
    public string Name { get; set; }
    public string? Icon { get; set; }
    public CacheEmote? Emoji { get; set; }
    public CacheGuildPermissions Permissions { get; set; }
    public int Position { get; set; }
    public RoleTags? Tags { get; set; }
}

public static class CacheRoleExtensions
{
    public static void UpdateValues(this ICacheRole instance, IRole? role)
    {
        if (role == null) return;
        instance.RoleId = role.Id;
        instance.GuildId = role.Guild.Id;
        instance.Color = XeniaHelper.ToHex(role.Color);
        instance.IsHoisted = role.IsHoisted;
        instance.IsManaged = role.IsManaged;
        instance.IsMentionable = role.IsMentionable;
        instance.Name = role.Name;
        instance.Icon = role.Icon;
        instance.Emoji = CacheEmote.FromExisting(role.Emoji);
        instance.Permissions = new CacheGuildPermissions(role.Permissions);
        instance.Position = role.Position;
        instance.Tags = role.Tags;
    }

    public static void UseDefaultValues(this ICacheRole instance)
    {
        instance.RoleId = null;
        instance.GuildId = 0;
        instance.Color = "000000";
        instance.IsHoisted = false;
        instance.IsManaged = false;
        instance.IsMentionable = true;
        instance.Name = "";
        instance.Icon = null;
        instance.Emoji = null;
        instance.Permissions = CacheGuildPermissions.None;
        instance.Position = 0;
        instance.Tags = null;
    }
}