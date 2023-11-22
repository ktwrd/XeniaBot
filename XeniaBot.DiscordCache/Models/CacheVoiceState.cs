using Discord;

namespace XeniaBot.DiscordCache.Models;

/// <summary>
///     Represents a WebSocket user's voice connection status. Mutable version of <see cref="IVoiceState"/>
/// </summary>
public class CacheVoiceState
{
    /// <summary>Gets the voice channel this user is currently in.</summary>
    /// <returns>
    ///     A generic voice channel object representing the voice channel that the user is currently in; <see langword="null" />
    ///     if none.
    /// </returns>
    public ulong VoiceChannelId { get; set; }
    /// <summary>
    ///     Gets the unique identifier for this user's voice session.
    /// </summary>
    public string VoiceSessionId { get; set; }
    /// <summary>
    ///     Gets the time on which the user requested to speak.
    /// </summary>
    public DateTimeOffset? RequestToSpeakTimestamp { get; set; }
    /// <summary>
    ///     Gets a value that indicates whether this user is muted (i.e. not permitted to speak via voice) by the
    ///     guild.
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> if this user is muted by the guild; otherwise <see langword="false" />.
    /// </returns>
    public bool IsMuted { get; set; }
    /// <summary>
    ///     Gets a value that indicates whether this user is deafened by the guild.
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> if the user is deafened (i.e. not permitted to listen to or speak to others) by the guild;
    ///     otherwise <see langword="false" />.
    /// </returns>
    public bool IsDeafened { get; set; }
    /// <summary>
    ///     Gets a value that indicates whether the user is muted by the current user.
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> if the guild is temporarily blocking audio to/from this user; otherwise <see langword="false" />.
    /// </returns>
    public bool IsSuppressed { get; set; }
    /// <summary>
    ///     Gets a value that indicates whether this user has marked themselves as muted (i.e. not permitted to
    ///     speak via voice).
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> if this user has muted themselves; otherwise <see langword="false" />.
    /// </returns>
    public bool IsSelfMuted { get; set; }
    /// <summary>
    ///     Gets a value that indicates whether this user has marked themselves as deafened.
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> if this user has deafened themselves (i.e. not permitted to listen to or speak to others); otherwise <see langword="false" />.
    /// </returns>
    public bool IsSelfDeafened { get; set; }
    /// <summary>
    ///     Gets a value that indicates if this user is streaming in a voice channel.
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> if the user is streaming; otherwise <see langword="false" />.
    /// </returns>
    public bool IsStreaming { get; set; }
    /// <summary>
    ///     Gets a value that indicates if the user is videoing in a voice channel.
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> if the user has their camera turned on; otherwise <see langword="false" />.
    /// </returns>
    public bool IsVideoing { get; set; }

    public CacheVoiceState Update(IVoiceState state)
    {
        this.VoiceChannelId = state.VoiceChannel.Id;
        this.VoiceSessionId = state.VoiceSessionId;
        this.RequestToSpeakTimestamp = state.RequestToSpeakTimestamp;
        this.IsMuted = state.IsMuted;
        this.IsDeafened = state.IsDeafened;
        this.IsSuppressed = state.IsSuppressed;
        this.IsSelfMuted = state.IsSelfMuted;
        this.IsSelfDeafened = state.IsSelfDeafened;
        this.IsStreaming = state.IsStreaming;
        this.IsVideoing = state.IsVideoing;
        return this;
    }
    public static CacheVoiceState? FromExisting(IVoiceState? state)
    {
        if (state == null)
            return null;

        var instance = new CacheVoiceState();
        return instance.Update(state);
    }
}