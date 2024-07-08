using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheUserModelData
    : IUser
{
    public static CacheUserModelData? FromModel(CacheUserModel? model)
    {
        if (model == null)
            return null;
        var i = new CacheUserModelData();
        // IPresence
        i.Status = model.Status;
        i.ActiveClients = model.ActiveClients;
        i.Activities = model.Activities;
        
        // ISnowflakeEntity
        i.Id = model.Snowflake;
        i.CreatedAt = model.CreatedAt;
        
        // IUser
        i.AvatarId = model.AvatarId;
        i.Discriminator = model.Discriminator;
        i.DiscriminatorValue = model.DiscriminatorValue;

        i.IsBot = model.IsBot;
        i.IsWebhook = model.IsWebhook;
        i.Username = model.Username;
        i.PublicFlags = model.PublicFlags;
        i.GlobalName = model.GlobalName;
        i.AvatarDecorationHash = model.AvatarDecorationHash;
        i.AvatarDecorationSkuId = model.AvatarDecorationSkuId;

        return i;
    }
    #region IPresence
    public UserStatus Status { get; set; }
    public IReadOnlyCollection<ClientType> ActiveClients { get; set; }
    public IReadOnlyCollection<IActivity> Activities { get; set; }
    #endregion
    #region IMentionable

    public string Mention => $"<@{Id}>";
    #endregion
    #region ISnowflakeEntity
    public ulong Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    #endregion
    #region IUser
    public string AvatarId { get; set; }

    public string GetAvatarUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128)
    {
        return CDN.GetUserAvatarUrl(Id, AvatarId, size, format);
    }

    public string GetDefaultAvatarUrl()
    {
        return this.DiscriminatorValue == (ushort) 0 ? CDN.GetDefaultUserAvatarUrl(this.Id) : CDN.GetDefaultUserAvatarUrl(this.DiscriminatorValue);
    }

    public string GetDisplayAvatarUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128)
    {
        return GetAvatarUrl(format, size) ?? GetDefaultAvatarUrl();
    }
    public string Discriminator { get; set; }
    public ushort DiscriminatorValue { get; set; }
    public bool IsBot { get; set; }
    public bool IsWebhook { get; set; }
    public string Username { get; set; }
    public UserProperties? PublicFlags { get; set; }
    public string GlobalName { get; set; }
    public string AvatarDecorationHash { get; set; }
    public ulong? AvatarDecorationSkuId { get; set; }

    public Task<IDMChannel> CreateDMChannelAsync(RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public string GetAvatarDecorationUrl()
    {
        return this.AvatarDecorationHash == null ? (string) null : CDN.GetAvatarDecorationUrl(this.AvatarDecorationHash);
    }
    #endregion
}