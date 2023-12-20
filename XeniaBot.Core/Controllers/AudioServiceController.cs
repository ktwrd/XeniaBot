using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Victoria.Node;
using Victoria.Node.EventArgs;
using Victoria.Player;
using XeniaBot.Core.Helpers;
using XeniaBot.Shared;

namespace XeniaBot.Core.Controllers;

[BotController]
public class AudioServiceController : BaseController
{
    public LavaNode LavaNode { get; private set; }
    public bool Enable { get; private set; }
    private readonly DiscordSocketClient _client;
    private readonly ConfigData _configData;
    public readonly HashSet<ulong> VoteQueue;
    private readonly ConcurrentDictionary<ulong, CancellationTokenSource> _disconnectTokens;

    public AudioServiceController(IServiceProvider services)
        : base(services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        _configData = services.GetRequiredService<ConfigData>();

        Enable = _configData.Lavalink_Enable;

        if (_configData.Lavalink_Enable == false)
        {
            Log.Warn("ConfigData.Lavalink_Enable is false. Ignoring.");
        }
        else
        {
            if (_configData.Lavalink_Hostname == null)
            {
                Log.Error("ConfigData.Lavalink_Hostname is null. Please fix.");
                Environment.Exit(1);
                return;
            }
        }
        var config = new NodeConfiguration()
        {
            Hostname = _configData.Lavalink_Hostname,
            Port = _configData.Lavalink_Port,
            Authorization = _configData.Lavalink_Auth,
            IsSecure = _configData.Lavalink_Secure,
            EnableResume = true
        };
        LavaNode = new LavaNode(_client, config, new LoggerPolyfillT<LavaNode>());
        
        _disconnectTokens = new ConcurrentDictionary<ulong, CancellationTokenSource>();

        VoteQueue = new HashSet<ulong>();

        // LavaNode.OnTrackEnd += OnTrackEndAsync;
        // LavaNode.OnTrackStart += OnTrackStartAsync;
        // LavaNode.OnStatsReceived += OnStatsReceivedAsync;
        // LavaNode.OnUpdateReceived += OnUpdateReceivedAsync;
        // LavaNode.OnWebSocketClosed += OnWebSocketClosedAsync;
        // LavaNode.OnTrackStuck += OnTrackStuckAsync;
        // LavaNode.OnTrackException += OnTrackExceptionAsync;
    }

    public override async Task OnReady()
    {
        if (!Enable)
        {
            Log.WriteLine("Not going to connect to Lavalink server since it's disabled");
            return;
        }
        try
        {
            Log.WriteLine("Connecting to Lavalink Server");
            await LavaNode.ConnectAsync();
        }
        catch (Exception ex)
        {
            await DiscordHelper.ReportError(ex, "Failed to run LavaNode.ConnectAsync()");
        }
    }

    private static Task OnTrackExceptionAsync(TrackExceptionEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg) {
        arg.Player.Vueue.Enqueue(arg.Track);
        return arg.Player.TextChannel.SendMessageAsync($"{arg.Track} has been re-queued because it threw an exception.");
    }

    private static Task OnTrackStuckAsync(TrackStuckEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg) {
        arg.Player.Vueue.Enqueue(arg.Track);
        return arg.Player.TextChannel.SendMessageAsync($"{arg.Track} has been re-queued because it got stuck.");
    }

    private Task OnWebSocketClosedAsync(WebSocketClosedEventArg arg)
    {
        Log.Error($"{arg.Code} {arg.Reason}");
        return Task.CompletedTask;
    }

    private Task OnStatsReceivedAsync(StatsEventArg arg) {
        Log.WriteLine(JsonSerializer.Serialize(arg, Program.SerializerOptions));
        return Task.CompletedTask;
    }

    private static Task OnUpdateReceivedAsync(UpdateEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg) {
        return arg.Player.TextChannel.SendMessageAsync(
            $"Player update received: {arg.Position}/{arg.Track?.Duration}");
    }

    private static Task OnTrackStartAsync(TrackStartEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg) {
        return arg.Player.TextChannel.SendMessageAsync($"Started playing {arg.Track}.");
    }

    private static Task OnTrackEndAsync(TrackEndEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg) {
        return arg.Player.TextChannel.SendMessageAsync($"Finished playing {arg.Track}.");
    }
}