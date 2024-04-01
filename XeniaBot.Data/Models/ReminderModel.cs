﻿using System;
using kate.shared.Helpers;
using MongoDB.Bson.Serialization.Serializers;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models;

public class ReminderModel : BaseModel
{
    public string ReminderId { get; set; }
    /// <summary>
    /// User that created this reminder.
    /// </summary>
    public ulong UserId { get; set; }
    /// <summary>
    /// Guild where this reminder was created in.
    /// </summary>
    public ulong GuildId { get; set; }
    /// <summary>
    /// Channel where the user should be pinged in.
    /// </summary>
    public ulong ChannelId { get; set; }
    /// <summary>
    /// <para>Timestamp that this reminder was created at.</para>
    ///
    /// <para>Unix Timestamp (Milliseconds, UTC)</para>
    /// </summary>
    public long CreatedAt { get; set; }
    /// <summary>
    /// <para>Timestamp when we should notify the user at.</para>
    ///
    /// <para>Unix Timestamp (Seconds, UTC)</para>
    /// </summary>
    public long ReminderTimestamp { get; set; }
    /// <summary>
    /// <para>Have we notified the user of this reminder?</para>
    ///
    /// <para>Ignore sending notification/showing user when this is `true`.</para>
    /// </summary>
    public bool HasReminded { get; set; }
    /// <summary>
    /// Note created by user.
    /// </summary>
    public string Note { get; set; }
    /// <summary>
    /// <para>Timestamp when we notified the user of this reminder.</para>
    ///
    /// <para>Milliseconds since Unix Epoch (UTC)</para>
    /// </summary>
    public long RemindedAt { get; set; }
    /// <summary>
    /// Where was this reminder created? Bot or Dashboard.
    /// </summary>
    public RemindSource Source { get; set; }

    public ReminderModel()
    {
        ReminderId = GeneralHelper.GenerateUID();
        HasReminded = false;
        Note = "";
        CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public ReminderModel(
        ulong userId,
        ulong channelId,
        ulong guildId,
        long timestamp,
        RemindSource source,
        string? note = null)
    : base()
    {
        ReminderId = GeneralHelper.GenerateUID();
        UserId = userId;
        ChannelId = channelId;
        GuildId = guildId;
        ReminderTimestamp = timestamp;
        Note = note ?? "";
        HasReminded = false;
        Source = source;
    }
}

public enum RemindSource
{
    Unknown = -1,
    Bot = 0,
    Dashboard = 1
}