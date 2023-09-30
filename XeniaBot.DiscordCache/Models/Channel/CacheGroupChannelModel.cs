using Discord.WebSocket;

namespace XeniaBot.DiscordCache.Models;

public class CacheGroupChannelModel : CacheBaseChannel
{
    public string Name { get; set; }
    public string RTCRegion { get; set; }
    public ulong[] UserIds { get; set; }
    public ulong[] RecipientIds { get; set; }

    public CacheGroupChannelModel FromExisting(SocketGroupChannel channel)
    {
        base.FromExisting(channel);
        Name = channel.Name;
        RTCRegion = channel.RTCRegion;
        UserIds = channel.Users.Select(v => v.Id).ToArray();
        RecipientIds = channel.Recipients.Select(v => v.Id).ToArray();
        return this;
    }
}