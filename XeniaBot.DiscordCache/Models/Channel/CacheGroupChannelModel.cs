using Discord.WebSocket;

namespace XeniaBot.DiscordCache.Models;

public class CacheGroupChannelModel : CacheBaseChannel
{
    public string Name { get; set; }
    public string RTCRegion { get; set; }
    public ulong[] UserIds { get; set; }
    public ulong[] RecipientIds { get; set; }

    public CacheGroupChannelModel Update(SocketGroupChannel channel)
    {
        base.Update(channel);
        Name = channel.Name;
        RTCRegion = channel.RTCRegion;
        UserIds = channel.Users.Select(v => v.Id).ToArray();
        RecipientIds = channel.Recipients.Select(v => v.Id).ToArray();
        return this;
    }

    public static CacheGroupChannelModel? FromExisting(SocketGroupChannel? channel)
    {
        if (channel == null)
            return null;

        var instance = new CacheGroupChannelModel();
        return instance.Update(channel);
    }
}