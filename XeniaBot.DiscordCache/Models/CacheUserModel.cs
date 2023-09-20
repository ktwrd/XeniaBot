using CacheeniaBot.DiscordCache.Helpers;
using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheUserModel
    : DiscordCacheBaseModel,
        IMentionable
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

    public new CacheUserModel FromExisting(IUser? user)
    {
        this.Snowflake = user.Id;
        this.CreatedAt = user.CreatedAt;
        this.AvatarId = user.AvatarId;
        this.Discriminator = user.Discriminator;
        this.DiscriminatorValue = user.DiscriminatorValue;
        this.IsBot = user.IsBot;
        this.IsWebhook = user.IsWebhook;
        this.Username = user.Username;
        this.PublicFlags = user.PublicFlags;
        this.Mention = user.Mention;
        this.Status = user.Status;
        this.ActiveClients = user.ActiveClients.ToArray();
        this.Activities = DiscordCacheHelper.ForceTypeCast<IReadOnlyCollection<IActivity>, CacheUserActivity[]>(user.Activities) ?? Array.Empty<CacheUserActivity>();
        return this;
    }
    public static CacheUserModel? FromUser(IUser? user)
    {
        if (user == null)
            return null;
        return new CacheUserModel().FromExisting(user);
    }
}