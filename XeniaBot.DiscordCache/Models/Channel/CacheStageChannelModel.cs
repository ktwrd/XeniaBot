using Discord;
using Discord.WebSocket;

namespace XeniaBot.DiscordCache.Models;

public class CacheStageChannelModel : CacheVoiceChannelModel
{
    public StagePrivacyLevel? PrivacyLevel { get; set; }
    public bool? IsDiscoverableDisabled { get; set; }
    public bool IsLive { get; set; }
    public ulong[] SpeakerIds { get; set; }
    public new CacheStageChannelModel Update(SocketStageChannel channel)
    {
        base.Update(channel);
        PrivacyLevel = channel.PrivacyLevel;
        IsDiscoverableDisabled = channel.IsDiscoverableDisabled;
        IsLive = channel.IsLive;
        SpeakerIds = channel.Speakers.Select(v => v.Id).ToArray();
        return this;
    }

    public static CacheStageChannelModel? FromExisting(SocketStageChannel? channel)
    {
        if (channel == null)
            return null;

        var instance = new CacheStageChannelModel();
        return instance.Update(channel);
    }
}