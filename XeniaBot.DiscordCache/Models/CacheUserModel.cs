using CacheeniaBot.DiscordCache.Helpers;
using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheUserModel
    : DiscordCacheBaseModel, IMentionable
{
    #region ISnowflakeEntity
    public DateTimeOffset CreatedAt { get; set; }
    #endregion
    
    #region IUser
    public string AvatarId { get; set; }
    public string Discriminator { get; set; }
    public ushort DiscriminatorValue { get; set; }
    
    public bool IsBot { get; set; }
    public bool IsWebhook { get; set; }
    public string Username { get; set; }
    
    public UserProperties? PublicFlags { get; set; }
    
    #region IMentionable
    public string Mention { get; set; }
    #endregion
    
    #region IPresence
    public UserStatus Status { get; set; }
    public ClientType[] ActiveClients { get; set; }
    public CacheUserActivity[] Activities { get; set; }
    #endregion
    #endregion

    public CacheUserModel()
    {
        ActiveClients = Array.Empty<ClientType>();
        Activities = Array.Empty<CacheUserActivity>();
    }

    public static CacheUserModel? FromUser(IUser? user)
    {
        if (user == null)
            return null;
        var instance = new CacheUserModel();
        instance.Snowflake = user.Id;
        instance.CreatedAt = user.CreatedAt;
        instance.AvatarId = user.AvatarId;
        instance.Discriminator = user.Discriminator;
        instance.DiscriminatorValue = user.DiscriminatorValue;
        instance.IsBot = user.IsBot;
        instance.IsWebhook = user.IsWebhook;
        instance.Username = user.Username;
        instance.PublicFlags = user.PublicFlags;
        instance.Mention = user.Mention;
        instance.Status = user.Status;
        instance.ActiveClients = user.ActiveClients.ToArray();
        instance.Activities = DiscordCacheHelper.ForceTypeCast<IReadOnlyCollection<IActivity>, CacheUserActivity[]>(user.Activities) ?? Array.Empty<CacheUserActivity>();
        return instance;
    }
}