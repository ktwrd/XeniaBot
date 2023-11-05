using Discord;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.DiscordCache.Models;

public class CacheRole
{
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
        GuildId = role.Guild.Id;
        Color = XeniaHelper.ToHex(role.Color);
        IsHoisted = role.IsHoisted;
        IsManaged = role.IsManaged;
        IsMentionable = role.IsMentionable;
        Name = role.Name;
        Icon = role.Icon;
        Emoji = CacheEmote.FromExisting(role.Emoji);
        Permissions = new CacheGuildPermissions(role.Permissions);
        Position = role.Position;
        Tags = role.Tags;
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