﻿using Discord.WebSocket;

namespace XeniaBot.WebPanel.Models;

public class StrippedGuild
{
    /// <summary>
    /// Name of the guild
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Amount of members in guild
    /// </summary>
    public int MemberCount { get; set; }
    /// <summary>
    /// UserId of the guild owner
    /// </summary>
    public ulong OwnerId { get; set; }
    /// <summary>
    /// Icon Url for this guild. Will default to `~/Debugmissing.png` when is null.
    /// </summary>
    public string IconUrl { get; set; }
    /// <summary>
    /// Banner Url for this guild
    /// </summary>
    public string? BannerUrl { get; set; }
    /// <summary>
    /// Description of this guild.
    /// </summary>
    public string Description { get; set; }

    public static StrippedGuild FromGuild(SocketGuild? guild)
    {
        if (guild == null)
        {
            return new StrippedGuild()
            {
                Name = "<unknown>",
                MemberCount = -1,
                OwnerId = 0,
                IconUrl = "~/Debugmissing.png",
                Description = ""
            };
        }
        else
        {
            var instance = new StrippedGuild();
            instance.Name = guild.Name;
            instance.MemberCount = guild.MemberCount;
            instance.OwnerId = guild.OwnerId;
            instance.IconUrl = guild.IconUrl ?? "~/Debugmissing.png";
            instance.BannerUrl = guild.BannerUrl;
            instance.Description = guild.Description ?? "";
            return instance;
        }
    }
}