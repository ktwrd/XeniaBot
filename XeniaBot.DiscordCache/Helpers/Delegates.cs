using XeniaBot.DiscordCache.Models;

namespace XeniaBot.Data.Models.Archival;

public enum MessageChangeType
{
    Create,
    Delete,
    Update
}

public enum UserChangeType
{
    Update,
    Delete
}
public delegate void MessageDiffDelegate(
    MessageChangeType type,
    CacheMessageModel current,
    CacheMessageModel? previous);
    
public delegate void UserDiffDelegate(
    UserChangeType type,
    CacheUserModel current,
    CacheUserModel? previous);