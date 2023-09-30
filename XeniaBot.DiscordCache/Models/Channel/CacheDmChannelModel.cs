using Discord.WebSocket;

namespace XeniaBot.DiscordCache.Models;

public class CacheDmChannelModel : CacheBaseChannel
{
    public string Name { get; set; }
    public CacheUserModel Recipient { get; set; }
    public new CacheDmChannelModel FromExisting(SocketDMChannel channel)
    {
        base.FromExisting(channel);
        Name = channel.Recipient.Username;
        Recipient = CacheUserModel.FromUser(channel.Recipient);
        IsDmChannel = true;
        return this;
    }
}