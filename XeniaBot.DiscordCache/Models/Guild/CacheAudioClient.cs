using Discord;
using Discord.Audio;

namespace XeniaBot.DiscordCache.Models;

public class CacheAudioClient
{
    public ConnectionState ConnectionState { get; set; }
    public int Latency { get; set; }
    public int UdpLatency { get; set; }
    public CacheAudioClient? FromExisting(IAudioClient audioClient)
    {
        if (audioClient == null)
            return null;
        ConnectionState = audioClient.ConnectionState;
        Latency = audioClient.Latency;
        UdpLatency = audioClient.UdpLatency;
        return this;
    }
}