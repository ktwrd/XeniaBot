using XeniaBot.DiscordCache.Models;

namespace XeniaBot.Data.Models.Archival;

public enum MessageChangeType
{
    Create,
    Delete,
    Update
}

public enum CacheChangeType
{
    Update,
    Delete,
    Create
}
public delegate void MessageDiffDelegate(
    MessageChangeType type,
    CacheMessageModel current,
    CacheMessageModel? previous);
    
public delegate void UserDiffDelegate(
    CacheChangeType type,
    CacheUserModel current,
    CacheUserModel? previous);
public delegate void GuildMemberDiffDelegate(
    CacheChangeType type,
    CacheGuildMemberModel current,
    CacheGuildMemberModel? previous);
public delegate void GuildDiffDelegate(
    CacheChangeType type,
    CacheGuildModel current,
    CacheGuildModel? previous);
public delegate void ChannelDiffDelegate(
    CacheChangeType type,
    CacheGuildChannelModel current,
    CacheGuildChannelModel? previous);