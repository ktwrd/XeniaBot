using System;
using System.Collections.Generic;
using Discord;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models;

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
public class ServerLogModel : BaseModel
{
    public ulong ServerId;
    public ulong DefaultLogChannel;
    public ulong? MemberJoinChannel = null;
    public ulong? MemberLeaveChannel = null;
    public ulong? MemberBanChannel = null;
    public ulong? MemberKickChannel = null;
    public ulong? MessageEditChannel = null;
    public ulong? MessageDeleteChannel = null;
    public ulong? ChannelCreateChannel = null;
    public ulong? ChannelEditChannel = null;
    public ulong? ChannelDeleteChannel = null;
    public ulong? MemberVoiceChangeChannel = null;

    public ulong GetChannel(ServerLogEvent logEvent)
    {
        var dict = GetAsDictionary();
        dict.TryGetValue(logEvent, out var targetChannel);
        return targetChannel ?? DefaultLogChannel;
    }

    /// <summary>
    /// Fetch ServerLogModel as a dictionary
    /// </summary>
    public Dictionary<ServerLogEvent, ulong?> GetAsDictionary()
    {
        return new Dictionary<ServerLogEvent, ulong?>()
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
                throw new Exception($"LogEvent {logEvent} not implemented in database");
        }
    }
}