using System;
using System.Collections.Generic;
using XeniaBot.Shared.Models;

namespace XeniaBot.MongoData.Models;

[Obsolete("Use XeniaDiscord.Data.Models.ServerLog.ServerLogEvent")]
public enum ServerLogEvent
{
    Fallback,
    MemberJoin,
    MemberLeave,
    MemberBan,
    MemberKick,
    MessageEdit,
    MessageDelete,
    ChannelDelete,
    ChannelEdit,
    ChannelCreate,
    MemberVoiceChange
}
[Obsolete("Use XeniaDiscord.Data.Models.ServerLog.ServerLogGuildModel")]
public class ServerLogModel : BaseModel
{
    public ulong ServerId { get; set; }
    public ulong DefaultLogChannel { get; set; }
    public ulong? MemberJoinChannel { get; set; } = null;
    public ulong? MemberLeaveChannel { get; set; } = null;
    public ulong? MemberBanChannel { get; set; } = null;
    public ulong? MemberKickChannel { get; set; } = null;
    public ulong? MessageEditChannel { get; set; } = null;
    public ulong? MessageDeleteChannel { get; set; } = null;
    public ulong? ChannelCreateChannel { get; set; } = null;
    public ulong? ChannelEditChannel { get; set; } = null;
    public ulong? ChannelDeleteChannel { get; set; } = null;
    public ulong? MemberVoiceChangeChannel { get; set; } = null;

    public ulong GetChannel(ServerLogEvent logEvent)
    {
        var dict = new Dictionary<ServerLogEvent, ulong?>()
        {
            {ServerLogEvent.MemberJoin, MemberJoinChannel},
            {ServerLogEvent.MemberLeave, MemberLeaveChannel},
            {ServerLogEvent.MemberBan, MemberBanChannel},
            {ServerLogEvent.MemberKick, MemberKickChannel},
            {ServerLogEvent.MessageEdit, MessageEditChannel},
            {ServerLogEvent.MessageDelete, MessageDeleteChannel},
            {ServerLogEvent.ChannelCreate, ChannelCreateChannel},
            {ServerLogEvent.ChannelEdit, ChannelEditChannel},
            {ServerLogEvent.ChannelDelete, ChannelDeleteChannel},
            {ServerLogEvent.MemberVoiceChange, MemberVoiceChangeChannel}
        };
        dict.TryGetValue(logEvent, out var targetChannel);
        return targetChannel ?? DefaultLogChannel;
    }

    public void SetChannel(ServerLogEvent logEvent, ulong? channelId)
    {
        
        switch (logEvent)
        {
            case ServerLogEvent.Fallback:
                DefaultLogChannel = channelId ?? 0;
                break;
            case ServerLogEvent.MemberJoin:
                MemberJoinChannel = channelId;
                break;
            case ServerLogEvent.MemberLeave:
                MemberLeaveChannel = channelId;
                break;
            case ServerLogEvent.MemberBan:
                MemberBanChannel = channelId;
                break;
            case ServerLogEvent.MemberKick:
                MemberKickChannel = channelId;
                break;
            case ServerLogEvent.MessageEdit:
                MessageEditChannel = channelId;
                break;
            case ServerLogEvent.MessageDelete:
                MessageDeleteChannel = channelId;
                break;
            case ServerLogEvent.ChannelCreate:
                ChannelCreateChannel = channelId;
                break;
            case ServerLogEvent.ChannelEdit:
                ChannelEditChannel = channelId;
                break;
            case ServerLogEvent.ChannelDelete:
                ChannelDeleteChannel = channelId;
                break;
            case ServerLogEvent.MemberVoiceChange:
                MemberVoiceChangeChannel = channelId;
                break;
            default:
                throw new NotImplementedException($"LogEvent {logEvent} not implemented in database");
        }
    }
}