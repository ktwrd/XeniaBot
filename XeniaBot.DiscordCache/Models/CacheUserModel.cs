using Discord;
using MongoDB.Bson.Serialization.Attributes;

namespace XeniaBot.DiscordCache.Models;

public class CacheUserModel
    : DiscordCacheBaseModel
    , IMentionable
    , ICacheUserModel
{
    public static string CollectionName => "cache_store_user";

#region ICacheUserModel
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
    [BsonIgnoreIfNull]
    public CacheUserPrimaryGuild? PrimaryGuild { get; set; }

    #region IMentionable
    public string Mention { get; set; }
    #endregion

    #region IPresence
    public UserStatus Status { get; set; }
    public ClientType[] ActiveClients { get; set; }
    public CacheUserActivity[] Activities { get; set; }
    #endregion
    #endregion
#endregion

    public CacheUserModel()
    {
        ActiveClients = Array.Empty<ClientType>();
        Activities = Array.Empty<CacheUserActivity>();
    }

    public virtual void Update(IUser user)
    {
        this.UpdateValues(user);
    }

    public static CacheUserModel? FromExisting(IUser? user)
    {
        if (user == null)
            return null;

        var instance = new CacheUserModel();
        instance.Update(user);
        return instance;
    }
}

public static class CacheUserModelExtensions
{
    public static void UpdateValues(this ICacheUserModel instance, IUser? user)
    {
        if (user == null) return;

        instance.Snowflake = user.Id;
        instance.CreatedAt = user.CreatedAt;
        instance.AvatarId = user.AvatarId;
        instance.Discriminator = user.Discriminator;
        instance.DiscriminatorValue = user.DiscriminatorValue;
        instance.IsBot = user.IsBot;
        instance.IsWebhook = user.IsWebhook;
        instance.Username = user.Username;
        instance.GlobalName = user.GlobalName;
        instance.AvatarDecorationHash = user.AvatarDecorationHash;
        instance.AvatarDecorationSkuId = user.AvatarDecorationSkuId;
        instance.PublicFlags = user.PublicFlags;
        instance.Mention = user.Mention;
        instance.Status = user.Status;
        instance.ActiveClients = user.ActiveClients.ToArray();
        instance.Activities = user.Activities
            .Select(CacheUserActivity.FromExisting)
            .Where(v => v != null)
            .Cast<CacheUserActivity>().ToArray();
        instance.PrimaryGuild = user.PrimaryGuild == null ? null : new CacheUserPrimaryGuild(user.PrimaryGuild.Value);
    }
}

public interface ICacheUserModel : IDiscordCacheBaseModel
{
    #region ISnowflakeEntity
    DateTimeOffset CreatedAt { get; set; }
    #endregion

    #region IUser
    string AvatarId { get; set; }
    string Discriminator { get; set; }
    ushort DiscriminatorValue { get; set; }

    bool IsBot { get; set; }
    bool IsWebhook { get; set; }
    string Username { get; set; }
    string GlobalName { get; set; }
    string AvatarDecorationHash { get; set; }
    [BsonIgnoreIfNull]
    ulong? AvatarDecorationSkuId { get; set; }
    [BsonIgnoreIfNull]
    UserProperties? PublicFlags { get; set; }
    [BsonIgnoreIfNull]
    CacheUserPrimaryGuild? PrimaryGuild { get; set; }

    #region IMentionable
    string Mention { get; set; }
    #endregion

    #region IPresence
    UserStatus Status { get; set; }
    ClientType[] ActiveClients { get; set; }
    CacheUserActivity[] Activities { get; set; }
    #endregion
    #endregion
}