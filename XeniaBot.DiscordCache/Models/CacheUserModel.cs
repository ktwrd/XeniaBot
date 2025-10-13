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

    public CacheUserModel()
    {
        ActiveClients = Array.Empty<ClientType>();
        Activities = Array.Empty<CacheUserActivity>();
    }

    public CacheUserModel Update(IUser user)
    {
        Snowflake = user.Id;
        CreatedAt = user.CreatedAt;
        AvatarId = user.AvatarId;
        Discriminator = user.Discriminator;
        DiscriminatorValue = user.DiscriminatorValue;
        IsBot = user.IsBot;
        IsWebhook = user.IsWebhook;
        Username = user.Username;
        GlobalName = user.GlobalName;
        AvatarDecorationHash = user.AvatarDecorationHash;
        AvatarDecorationSkuId = user.AvatarDecorationSkuId;
        PublicFlags = user.PublicFlags;
        Mention = user.Mention;
        Status = user.Status;
        ActiveClients = user.ActiveClients.ToArray();
        Activities = user.Activities
            .Select(CacheUserActivity.FromExisting)
            .Where(v => v != null)
            .Cast<CacheUserActivity>().ToArray();
        PrimaryGuild = user.PrimaryGuild == null ? null : new CacheUserPrimaryGuild(user.PrimaryGuild.Value);
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