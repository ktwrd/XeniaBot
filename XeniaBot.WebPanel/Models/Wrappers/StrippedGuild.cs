using System;
using Discord.WebSocket;
using XeniaDiscord.Data.Models.Snapshot;

namespace XeniaBot.WebPanel.Models;

public sealed class StrippedGuild
    : IEquatable<StrippedGuild>
{
    public bool Equals(StrippedGuild? other)
    {
        return other?.Id == Id;
    }
    
    public ulong Id { get; set; }

    /// <summary>
    /// Name of the guild
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Amount of members in guild
    /// </summary>
    public int MemberCount { get; set; }
    /// <summary>
    /// UserId of the guild owner
    /// </summary>
    public ulong OwnerId { get; set; }
    /// <summary>
    /// Icon Url for this guild. Will default to `/Debugempty.png` when is null.
    /// </summary>
    public string? IconUrl { get; set; }
    /// <summary>
    /// Banner Url for this guild
    /// </summary>
    public string? BannerUrl { get; set; }
    /// <summary>
    /// Description of this guild.
    /// </summary>
    public string? Description { get; set; }

    public static StrippedGuild FromGuild(SocketGuild? guild)
    {
        if (guild == null)
        {
            return new StrippedGuild()
            {
                Id = 0,
                Name = "<unknown>",
                MemberCount = -1,
                OwnerId = 0,
                IconUrl = "/Debugempty.png",
                Description = ""
            };
        }
        else
        {
            return new StrippedGuild
            {
                Id = guild.Id,
                Name = guild.Name,
                MemberCount = guild.MemberCount,
                OwnerId = guild.OwnerId,
                IconUrl = guild.IconUrl ?? "/Debugempty.png",
                BannerUrl = guild.BannerUrl,
                Description = guild.Description ?? ""
            };
        }
    }

    public static StrippedGuild FromExisting(
        SocketGuild? guild,
        GuildSnapshotModel? model,
        ulong id)
    {
        if (guild != null) return FromGuild(guild);
        if (model == null)
            return new StrippedGuild()
            {
                Id = id,
                Name = id.ToString(),
                MemberCount = -1,
                OwnerId = 0,
                IconUrl = "/Debugempty.png",
            };
        return new StrippedGuild()
        {
            Id = model.GetGuildId(),
            Name = model.Name,
            MemberCount = model.ApproximateMemberCount ?? -1,
            OwnerId = model.GetOwnerUserId(),
            IconUrl = model.IconUrl ?? "/Debugempty.png",
            Description = model.Description
        };
    }
}