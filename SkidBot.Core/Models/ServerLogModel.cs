using System;
using System.Collections.Generic;
using Discord;

namespace SkidBot.Core.Models;

public enum ServerLogEvent
{
    Fallback,
    Join,
    Leave,
    Ban,
    Kick,
    MessageEdit,
    MessageDelete
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

}