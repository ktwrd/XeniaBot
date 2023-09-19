using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheRole
{
    public ulong GuildId { get; set; }
    public Discord.Color Color { get; set; }
    public bool IsHoisted { get; set; }
    public bool IsManaged { get; set; }
    public bool IsMentionable { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public CacheEmote Emoji { get; set; }
    public GuildPermissions Permissions { get; set; }
    public int Position { get; set; }
    public RoleTags Tags { get; set; }

    public CacheRole FromExisting(IRole role)
    {
        GuildId = role.Guild.Id;
        Color = role.Color;
        IsHoisted = role.IsHoisted;
        IsManaged = role.IsManaged;
        IsMentionable = role.IsMentionable;
        Name = role.Name;
        Icon = role.Icon;
        Emoji = new CacheEmote().FromExisting(role.Emoji);
        Permissions = role.Permissions;
        Position = role.Position;
        Tags = role.Tags;
        return this;
    }
}