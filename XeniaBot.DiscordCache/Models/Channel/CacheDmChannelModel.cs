using Discord.WebSocket;

namespace XeniaBot.DiscordCache.Models;

public class CacheDmChannelModel : CacheBaseChannel
{
    public string Name { get; set; }
    public CacheUserModel? Recipient { get; set; }
    public new CacheDmChannelModel Update(SocketDMChannel channel)
    {
        base.Update(channel);
        Name = channel.Recipient.Username;
        Recipient = CacheUserModel.FromExisting(channel.Recipient);
        IsDmChannel = true;
        return this;
    }

    public static CacheDmChannelModel? FromExisting(SocketDMChannel? channel)
    {
        if (channel == null)
            return null;

        var instance = new CacheDmChannelModel();
        return instance.Update(channel);
    }
}