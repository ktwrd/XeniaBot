using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Victoria;
using Victoria.Node;
using Victoria.Player;
using Victoria.Resolvers;
using Victoria.Responses.Search;
using XeniaBot.Core.Controllers;
using XeniaBot.Core.Helpers;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.Core.Modules;

public class AudioModule : InteractionModuleBase
{
    private LavaNode _lavaNode => Program.Services.GetRequiredService<AudioServiceController>().LavaNode;

    private AudioServiceController _audioServiceController =>
        Program.Services.GetRequiredService<AudioServiceController>();
    private static readonly IEnumerable<int> Range = Enumerable.Range(1900, 2000);

    // [SlashCommand("join", "Join your current voice channel")]
    // public async Task JoinAsync() {
    //     if (_lavaNode.HasPlayer(Context.Guild)) {
    //         await RespondAsync(embed: BaseEmbed()
    //             .WithDescription("I'm already connected to a voice channel!")
    //             .WithColor(Color.Red)
    //             .Build());
    //         return;
    //     }
    //
    //     var voiceState = Context.User as IVoiceState;
    //     if (voiceState?.VoiceChannel == null) {
    //         await RespondAsync(embed: BaseEmbed()
    //             .WithDescription("You must be connected to a voice channel!")
    //             .WithColor(Color.Red)
    //             .Build());
    //         return;
    //     }
    //
    //     try {
    //         await _lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
    //         await RespondAsync(embed: BaseEmbed()
    //             .WithDescription($"Joined {voiceState.VoiceChannel.Name}!")
    //             .Build());
    //     }
    //     catch (Exception exception) {
    //         await RespondAsync(
    //             embed: BaseEmbed()
    //                 .WithDescription($"Failed to join channel!\n`{exception.Message}`")
    //                 .WithColor(Color.Red)
    //                 .Build());
    //         await DiscordHelper.ReportError(exception, Context);
    //     }
    // }

    [SlashCommand("leave", "Leave voice channel.")]
    public async Task LeaveAsync() {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player)) {
            await RespondAsync(embed: BaseEmbed()
                .WithDescription("I'm not connected to any voice channels!")
                .WithColor(Color.Red)
                .Build());
            return;
        }

        var voiceChannel = (Context.User as IVoiceState).VoiceChannel ?? player.VoiceChannel;
        if (voiceChannel == null) {
            await RespondAsync(embed: BaseEmbed()
                .WithDescription("Not sure which voice channel to disconnect from.")
                .WithColor(Color.Red)
                .Build());
            return;
        }

        try {
            await _lavaNode.LeaveAsync(voiceChannel);
            await RespondAsync(embed: BaseEmbed()
                .WithDescription($"Left <#{voiceChannel.Id}>")
                .Build());
        }
        catch (Exception exception) {
            await RespondAsync(
                embed: BaseEmbed()
                    .WithDescription($"Failed to leave channel\n`{exception.Message}`")
                    .WithColor(Color.Red)
                    .Build());
            await DiscordHelper.ReportError(exception, Context);
        }
    }

    [SlashCommand("play", "Add track to queue and play it.")]
    public async Task PlayAsync([Remainder] string searchQuery)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                await RespondAsync(
                    embed: BaseEmbed()
                        .WithDescription("Please provide search terms.")
                        .WithColor(Color.Red)
                        .Build());
                return;
            }

            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                var voiceState = Context.User as IVoiceState;
                if (voiceState?.VoiceChannel == null)
                {
                    await RespondAsync(
                        embed: BaseEmbed()
                            .WithDescription("You must be connected to a voice channel!")
                            .WithColor(Color.Red)
                            .Build());
                    return;
                }

                try
                {
                    player = await _lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
                    await ReplyAsync(
                        embed: BaseEmbed()
                            .WithDescription($"Joined <#{voiceState.VoiceChannel.Id}>!")
                            .Build());
                }
                catch (Exception exception)
                {
                    await RespondAsync(
                        embed: BaseEmbed()
                            .WithDescription($"Failed to join your channel!\n`{exception.Message}`")
                            .WithColor(Color.Red)
                            .Build());
                    await DiscordHelper.ReportError(exception, Context);
                    return;
                }
            }

            var searchResponse = await _lavaNode.SearchAsync(
                Uri.IsWellFormedUriString(searchQuery, UriKind.Absolute) ? SearchType.Direct : SearchType.YouTube,
                searchQuery);
            if (searchResponse.Status is SearchStatus.LoadFailed or SearchStatus.NoMatches)
            {
                await RespondAsync(
                    embed: BaseEmbed()
                        .WithDescription($"I wasn't able to find anything for `{searchQuery}`.")
                        .WithColor(Color.Red)
                        .Build());
                return;
            }

            if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
            {
                player.Vueue.Enqueue(searchResponse.Tracks);
                await RespondAsync(
                    embed: BaseEmbed()
                        .WithDescription($"Enqueued {searchResponse.Tracks.Count} songs.")
                        .WithColor(Color.Red)
                        .Build());
            }
            else
            {
                var track = searchResponse.Tracks.FirstOrDefault();
                player.Vueue.Enqueue(track);

                await RespondAsync(
                    embed: BaseEmbed()
                        .WithDescription($"Added {track?.Title} to queue.")
                        .Build());
            }

            if (player.PlayerState is PlayerState.Playing or PlayerState.Paused)
            {
                return;
            }

            player.Vueue.TryDequeue(out var lavaTrack);
            await player.PlayAsync(lavaTrack);
        }
        catch (Exception ex)
        {
            await ReplyAsync(
                embed: BaseEmbed()
                    .WithDescription($"Failed to play track.\n`{ex.Message}`").WithColor(Color.Red)
                    .Build());
            await DiscordHelper.ReportError(ex, Context);
        }
    }

    [SlashCommand("pause", "Pause queue.")]
    [Command("Pause")]
    public async Task PauseAsync() {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player)) {
            await RespondAsync(embed: BaseEmbed()
                .WithDescription("I'm not connected to a voice channel.")
                .WithColor(Color.Red)
                .Build());
            return;
        }

        if (player.PlayerState != PlayerState.Playing) {
            await RespondAsync(embed: BaseEmbed()
                .WithDescription("I cannot pause when I'm not playing anything!")
                .WithColor(Color.Red)
                .Build());
            return;
        }

        try {
            await player.PauseAsync();
            await RespondAsync(embed: BaseEmbed()
                .WithDescription($"Paused: {player.Track.Title}")
                .Build());
        }
        catch (Exception exception) {
            await RespondAsync(
                embed: BaseEmbed()
                    .WithDescription($"Failed to pause queue!\n`{exception.Message}`")
                    .WithColor(Color.Red)
                    .Build());
            await DiscordHelper.ReportError(exception, Context);
        }
    }

    [SlashCommand("resume", "Resume queue after it's been paused.")]
    public async Task ResumeAsync() {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player)) {
            await RespondAsync(embed: BaseEmbed()
                .WithDescription("I'm not connected to a voice channel.")
                .WithColor(Color.Red)
                .Build());
            return;
        }

        if (player.PlayerState != PlayerState.Paused) {
            await RespondAsync(embed: BaseEmbed()
                .WithDescription("I cannot resume when I'm not playing anything!")
                .WithColor(Color.Red)
                .Build());
            return;
        }

        try {
            await player.ResumeAsync();
            await RespondAsync(embed: BaseEmbed()
                .WithDescription($"Resumed: {player.Track.Title}")
                .Build());
        }
        catch (Exception exception) {
            await RespondAsync(
                embed: BaseEmbed()
                    .WithDescription($"Failed to resume queue!\n`{exception.Message}`")
                    .WithColor(Color.Red)
                    .Build());
            await DiscordHelper.ReportError(exception, Context);
        }
    }

    [SlashCommand("stop", "Stop queue")]
    public async Task StopAsync() {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player)) {
            await RespondAsync(embed: BaseEmbed()
                .WithDescription("I'm not connected to a voice channel.")
                .WithColor(Color.Red)
                .Build());
            return;
        }

        if (player.PlayerState == PlayerState.Stopped) {
            await RespondAsync(embed: BaseEmbed()
                .WithDescription("Queue is already stopped.")
                .WithColor(Color.Red)
                .Build());
            return;
        }

        try {
            await player.StopAsync();
            await RespondAsync("No longer playing anything.");
        }
        catch (Exception exception) {
            await RespondAsync(
                embed: BaseEmbed()
                    .WithDescription($"Failed to stop queue!\n`{exception.Message}`")
                    .WithColor(Color.Red)
                    .Build());
            await DiscordHelper.ReportError(exception, Context);
        }
    }

    [SlashCommand("skip", "Skip current track.")]
    public async Task SkipAsync() {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player)) {
            await RespondAsync(embed: BaseEmbed()
                .WithDescription("I'm not connected to a voice channel.")
                .WithColor(Color.Red)
                .Build());
            return;
        }

        if (player.PlayerState != PlayerState.Playing) {
            await RespondAsync(embed: BaseEmbed()
                .WithDescription("Queue is empty, nothing to skip.")
                .WithColor(Color.Red)
                .Build());
            return;
        }

        // var voiceChannelUsers = (player.VoiceChannel.Guild as SocketGuild).Users
        //     .Where(x => !x.IsBot)
        //     .ToArray();
        // if (_audioServiceController.VoteQueue.Contains(Context.User.Id)) {
        //     await RespondAsync(embed: BaseEmbed()
        //         .WithDescription("You've already voted.")
        //         .WithColor(Color.Red)
        //         .Build());
        //     return;
        // }

        // _audioServiceController.VoteQueue.Add(Context.User.Id);
        // var percentage = _audioServiceController.VoteQueue.Count / voiceChannelUsers.Length * 100;
        // if (percentage < 85) {
        //     await RespondAsync(embed: BaseEmbed()
        //         .WithDescription("You need more than 85% votes to skip this song.")
        //         .WithColor(Color.Red)
        //         .Build());
        //     return;
        // }

        try {
            var (skipped, currenTrack) = await player.SkipAsync();
            await RespondAsync(embed: BaseEmbed()
                .WithDescription($"Skipped: {skipped.Title}\nNow Playing: {currenTrack.Title}")
                .Build());
        }
        catch (Exception exception) {
            await RespondAsync(
                embed: BaseEmbed()
                    .WithDescription($"Failed to skip track!\n`{exception.Message}`")
                    .WithColor(Color.Red)
                    .Build());
            await DiscordHelper.ReportError(exception, Context);
        }
    }

    [SlashCommand("seek", "Seek current track to specific time.")]
    public async Task SeekAsync(TimeSpan timeSpan) {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player)) {
            await RespondAsync(embed: BaseEmbed()
                .WithDescription("I'm not connected to a voice channel.")
                .WithColor(Color.Red)
                .Build());
            return;
        }

        if (player.PlayerState != PlayerState.Playing) {
            await RespondAsync(embed: BaseEmbed()
                .WithDescription("The queue is empty and nothing is playing, so I can't seek.")
                .WithColor(Color.Red)
                .Build());
            return;
        }

        try {
            await player.SeekAsync(timeSpan);
            await RespondAsync(embed: BaseEmbed()
                .WithDescription($"I've seeked `{player.Track.Title}` to {timeSpan}.")
                .Build());
        }
        catch (Exception exception) {
            await RespondAsync(
                embed: BaseEmbed()
                    .WithDescription($"Failed to seek track!\n`{exception.Message}`")
                    .WithColor(Color.Red)
                    .Build());
            await DiscordHelper.ReportError(exception, Context);
        }
    }

    [SlashCommand("volume", "Set the volume for the bot.")]
    public async Task VolumeAsync(ushort volume) {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player)) {
            await RespondAsync(
                embed: BaseEmbed()
                    .WithDescription("I'm not connected to a voice channel.")
                    .WithColor(Color.Orange)
                    .Build());
            return;
        }

        try {
            await player.SetVolumeAsync(volume);
            await RespondAsync(
                embed: BaseEmbed()
                    .WithDescription($"I've changed the player volume to {volume}.")
                    .Build());
        }
        catch (Exception exception) {
            await RespondAsync(
                embed: BaseEmbed()
                    .WithDescription($"Failed to set volume!\n`{exception.Message}`")
                    .WithColor(Color.Red)
                    .Build());
            await DiscordHelper.ReportError(exception, Context);
        }
    }

    [SlashCommand("nowplaying", "Show what track is currently playing in your voice chat.")]
    public async Task NowPlayingAsync() {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player)) {
            await RespondAsync(
                embed: BaseEmbed()
                    .WithDescription("I'm not connected to a voice channel.")
                    .WithColor(Color.Red)
                    .Build());
            return;
        }

        if (player.PlayerState != PlayerState.Playing)
        {
            await RespondAsync(
                embed: BaseEmbed()
                    .WithDescription("No track are currently playing, use `/play` to add a track to the queue!")
                    .WithColor(Color.Orange)
                    .Build());
            return;
        }

        var track = player.Track;
        var artwork = await track.FetchArtworkAsync();

        var embed = new EmbedBuilder()
            .WithAuthor(track.Author, Context.Client.CurrentUser.GetAvatarUrl(), track.Url)
            .WithTitle($"Now Playing: {track.Title}")
            .WithImageUrl(artwork)
            .WithFooter($"{track.Position}/{track.Duration}");

        await RespondAsync(embed: embed.Build());
    }

    [SlashCommand("queue", "Get track queue")]
    public async Task Queue()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player)) {
            await RespondAsync(
                embed: BaseEmbed()
                    .WithDescription("I'm not connected to a voice channel.")
                    .WithColor(Color.Red)
                    .Build());
            return;
        }

        var itemList = new List<string>();
        foreach (var item in player.Vueue)
        {
            itemList.Add($"- {item.Title}");
        }

        if (itemList.Count < 1)
        {
            await RespondAsync(embed: BaseEmbed().WithTitle("No items in queue").Build());
            return;
        }

        var chunked = ArrayHelper.ChunkLength(itemList, 4000);
        var embedList = new List<EmbedBuilder>();

        int totalTrackCount = chunked.Select(v => v.Length).Sum();
        for (int i = 0; i < chunked.Length; i++)
        {
            embedList.Add(new EmbedBuilder()
                .WithTitle($"Track Queue ({i / chunked[i].Length}/{totalTrackCount}, {i + 1} / {chunked.Length})")
                .WithDescription(string.Join("\n", chunked[i])));
        }

        var chunkedEmbeds = ArrayHelper.Chunk<EmbedBuilder>(embedList, 10);
        for (int i = 0; i < chunkedEmbeds.Length; i++)
        {
            if (i == 0)
            {
                await RespondAsync(embeds: chunkedEmbeds[i].Select(v => v.Build()).ToArray());
            }
            else
            {
                await ReplyAsync(embeds: chunkedEmbeds[i].Select(v => v.Build()).ToArray());
            }
        }
    }

    public EmbedBuilder BaseEmbed()
    {
        return new EmbedBuilder()
            .WithCurrentTimestamp()
            .WithColor(Color.Blue);
    }
}