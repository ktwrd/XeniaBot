using Discord;
using Discord.WebSocket;

namespace XeniaBot.DiscordCache.Models;

public class CacheVoiceChannelModel : CacheTextChannelModel
{
    public int Bitrate { get; set; }
    public int? UserLimit { get; set; }
    public string RTCRegion { get; set; }
    public ulong[] ConnectedUserIds { get; set; }
    public VideoQualityMode VideoQualityMode { get; set; }
    public new CacheVoiceChannelModel Update(SocketVoiceChannel channel)
    {
        base.Update(channel);
        Bitrate = channel.Bitrate;
        UserLimit = channel.UserLimit;
        RTCRegion = channel.RTCRegion;
        ConnectedUserIds = channel.ConnectedUsers.Select(v => v.Id).ToArray();
        VideoQualityMode = channel.VideoQualityMode;
        return this;
    }

    public static CacheVoiceChannelModel? FromExisting(SocketVoiceChannel? channel)
    {
        if (channel == null)
            return null;

        var instance = new CacheVoiceChannelModel();
        return instance.Update(channel);
    }
}