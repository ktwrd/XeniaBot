using System;
using System.Collections.Generic;
using Discord;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models;

public enum ServerLogEvent
{
    Fallback,
    Join,
    Leave,
    Ban,
    Kick,
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
        var dict = new Dictionary<ServerLogEvent, ulong?>()
        {
            {ServerLogEvent.Join, MemberJoinChannel},
            {ServerLogEvent.Leave, MemberLeaveChannel},
            {ServerLogEvent.Ban, MemberBanChannel},
            {ServerLogEvent.Kick, MemberKickChannel},
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
            case ServerLogEvent.Join:
                MemberJoinChannel = channelId;
                break;
            case ServerLogEvent.Leave:
                MemberLeaveChannel = channelId;
                break;
            case ServerLogEvent.Ban:
                MemberBanChannel = channelId;
                break;
            case ServerLogEvent.Kick:
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