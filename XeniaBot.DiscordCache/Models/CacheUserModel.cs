using XeniaBot.DiscordCache.Helpers;
using Discord;
using MongoDB.Bson.Serialization.Attributes;

namespace XeniaBot.DiscordCache.Models;

public class CacheUserModel
    : DiscordCacheBaseModel,
        IMentionable
{
    public static string CollectionName => "cache_store_user";
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
    public string GlobalName { get; set; }
    public string AvatarDecorationHash { get; set; }
    [BsonIgnoreIfNull]
    public ulong? AvatarDecorationSkuId { get; set; }
    [BsonIgnoreIfNull]

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

    public CacheUserModel Update(IUser user)
    {
        this.Snowflake = user.Id;
        this.CreatedAt = user.CreatedAt;
        this.AvatarId = user.AvatarId;
        this.Discriminator = user.Discriminator;
        this.DiscriminatorValue = user.DiscriminatorValue;
        this.IsBot = user.IsBot;
        this.IsWebhook = user.IsWebhook;
        this.Username = user.Username;
        this.GlobalName = user.GlobalName;
        this.AvatarDecorationHash = user.AvatarDecorationHash;
        this.AvatarDecorationSkuId = user.AvatarDecorationSkuId;
        this.PublicFlags = user.PublicFlags;
        this.Mention = user.Mention;
        this.Status = user.Status;
        this.ActiveClients = user.ActiveClients.ToArray();
        this.Activities = user.Activities
            .Select(CacheUserActivity.FromExisting)
            .Where(v => v != null)
            .Cast<CacheUserActivity>().ToArray();
        return this;
    }
    public static CacheUserModel? FromExisting(IUser? user)
    {
        if (user == null)
            return null;

        var instance = new CacheUserModel();
        return instance.Update(user);
    }
}