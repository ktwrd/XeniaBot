using Discord;
using Humanizer;
using System;
using System.Collections.Generic;

namespace XeniaBot.WebPanel.Areas.Admin.Models.Guild;

public class ListModel
{
    public IReadOnlyCollection<ListModelItem> Items { get; set; } = [];
}
public class ListModelItem
{
    public required ulong Id { get; set; }
    public string? Name { get; set; }
    public string? IconUrl { get; set; }
    public required bool IsMember { get; set; }
    /// <summary>
    /// only set if <see cref="IsMember"/> is <see langword="false"/>
    /// </summary>
    public required DateTime? RecordLastUpdatedAt { get; set; }
    public int? MemberCount { get; set; }
    public int? ChannelCount { get; set; }
    public int? RoleCount { get; set; }
    public required DateTime? JoinedAt { get; set; }
    public DateTime CreatedAt => SnowflakeUtils.FromSnowflake(Id).UtcDateTime;

    private static readonly TimeSpan OneMinuteAgo = TimeSpan.FromMinutes(1);
    public string? FormatJoinedAt(DateTime now)
    {
        if (!JoinedAt.HasValue) return null;

        var joinedAgo = now - JoinedAt.Value;
        if (joinedAgo >= OneMinuteAgo)
        {
            return joinedAgo.Humanize(3, maxUnit: TimeUnit.Year, minUnit: TimeUnit.Minute) + " ago";
        }
        return "< 1min ago";
    }
    public string FormatCreatedAt(DateTime now)
    {
        var createdAt = now - CreatedAt;
        if (createdAt >= OneMinuteAgo)
        {
            return createdAt.Humanize(3, maxUnit: TimeUnit.Year, minUnit: TimeUnit.Minute) + " ago";
        }
        return "< 1min ago";
    }
}
