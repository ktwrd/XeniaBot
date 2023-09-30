using Discord;
using Discord.WebSocket;

namespace XeniaBot.DiscordCache.Models;

public class CacheStageChannelModel : CacheVoiceChannelModel
{
    public StagePrivacyLevel? PrivacyLevel { get; set; }
    public bool? IsDiscoverableDisabled { get; set; }
    public bool IsLive { get; set; }
    public ulong[] SpeakerIds { get; set; }
    public new CacheStageChannelModel FromExisting(SocketStageChannel channel)
    {
        base.FromExisting(channel);
        PrivacyLevel = channel.PrivacyLevel;
        IsDiscoverableDisabled = channel.IsDiscoverableDisabled;
        IsLive = channel.IsLive;
        SpeakerIds = channel.Speakers.Select(v => v.Id).ToArray();
        return this;
    }
}