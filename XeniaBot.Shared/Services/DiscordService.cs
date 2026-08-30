using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sentry;
using XeniaBot.Shared.Helpers;

using LogSeverity = Discord.LogSeverity;

namespace XeniaBot.Shared.Services;

[XeniaController]
public class DiscordService
{
    private static readonly Logger Log = LogManager.GetLogger("Xenia.DiscordService");
    private readonly DiscordShardedClient _client;
    private readonly ConfigData _configData;
    private readonly InteractionHandler? _interactionHandler;
    private readonly ProgramDetails _details;
    public DiscordService(IServiceProvider services)
    {
        _details = services.GetRequiredService<ProgramDetails>();

        _configData = services.GetRequiredService<ConfigData>();
        _client = services.GetRequiredService<DiscordShardedClient>();

        if (_details.Platform == XeniaPlatform.Bot)
        {
            _interactionHandler = services.GetRequiredService<InteractionHandler>();
        }

        _client.Log += DiscordClientLogHandler;
        _client.ShardReady += OnShardReady;
        _client.ShardLatencyUpdated += ClientOnShardLatencyUpdated;
        // _client.Ready += OnClientReady;
        _client.MessageReceived += async (arg) =>
        {
            MessageReceived?.Invoke(arg);
        };
        _client.ShardDisconnected += OnClientDisconnected;
        // _client.Disconnected += OnClientDisconnected;
        // _client.LatencyUpdated += OnClientLatencyUpdated;
        CreateConnectionStatusThread();
        CreateLatencySanityCheckThread();
    }

    private readonly Dictionary<int, int> _shardLatency = [];
    private Dictionary<int, int> _publicShardLatency = [];
    public IReadOnlyDictionary<int, int> ShardLatency => _publicShardLatency;

    private Task ClientOnShardLatencyUpdated(int arg1, int arg2, DiscordSocketClient arg3)
    {
        var i = 0;
        foreach (var e in _client.Shards)
        {
            if (ReferenceEquals(e, arg3)) break;
            i++;
        }

        lock (_shardLatency)
        {
            _latencyLastUpdated = DateTimeOffset.UtcNow;
            _shardLatency[i] = arg2;
            _publicShardLatency = new Dictionary<int, int>(_shardLatency.Where(e => e.Key < _client.Shards.Count && e.Key >= 0));
        }
        return Task.CompletedTask;
    }

    public int ShardsReady { get; private set; }
    public int ShardCount => _client.Shards.Count;

    private async Task OnShardReady(DiscordSocketClient shard)
    {
        ShardsReady++;
        if (ShardsReady >= ShardCount)
        {
            await InvokeReady();
        }
    }

    private DateTimeOffset? _latencyLastUpdated;
    private DateTimeOffset? _readyAt;
    private Task OnClientDisconnected(Exception error, DiscordSocketClient client)
    {
        Log.Fatal(error, "Disconnected from Discord!!!");
        return Task.CompletedTask;
    }

    private void CreateConnectionStatusThread()
    {
        new Thread(() =>
        {
            try
            {
                ConnectionStatusThread().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Failed to run {nameof(ConnectionStatusThread)}");
                CreateConnectionStatusThread();
            }
        })
        {
            Name = $"{nameof(DiscordService)}.{nameof(ConnectionStatusThread)}"
        }.Start();
    }

    private async Task ConnectionStatusThread()
    {
        Log.Info("Created thread");
        await Task.Delay(60_000); // wait 1min before doing the reconnect stuff
        var connectingTime = 0;
        while (true)
        {
            // TODO rewrite this entire method. it's not compatible with DiscordShardedClient!
            // (ConnectionState is always Disconnected, but it's connected for the individual shard)
            await Task.Delay(15_000);
            continue;
            switch (_client.ConnectionState)
            {
                case ConnectionState.Disconnected:
                    connectingTime = 0;
                    try
                    {
                        await _client.StartAsync();
                    }
                    catch (Exception ex)
                    {
                        const string msg = "Failed to re-connect client (after disconnected for some reason)";
                        Log.Error(ex, msg);
                        SentrySdk.CaptureException(
                            new InvalidOperationException(msg,
                                ex));
                    }
                    await Task.Delay(2000);
                    break;
                case ConnectionState.Disconnecting:
                    await Task.Delay(1500);
                    break;
                case ConnectionState.Connecting:
                    await Task.Delay(1500);
                    connectingTime += 500;
                    if (connectingTime >= 15_000)
                    {
                        try
                        {
                            await _client.StopAsync();
                        }
                        catch (Exception ex)
                        {
                            const string msg = "Failed to disconnect after 15s of trying to re-connect";
                            Log.Error(ex, msg);
                            SentrySdk.CaptureException(
                                new InvalidOperationException(msg,
                                    ex));
                        }
                        await Task.Delay(500);
                        try
                        {

                            await _client.StartAsync();
                        }
                        catch (Exception ex)
                        {
                            const string msg = "Failed to reconnect after forceful disconnect (which happened after 15s of connecting)";
                            Log.Error(ex, msg);
                            SentrySdk.CaptureException(
                                new InvalidOperationException(msg,
                                    ex));
                        }
                    }
                    break;
                case ConnectionState.Connected:
                    connectingTime = 0;
                    await Task.Delay(5000);
                    break;
            }
        }
    }

    private void CreateLatencySanityCheckThread()
    {
        new Thread(() =>
        {
            try
            {
                LatencySanityCheckThread();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Failed to run {nameof(LatencySanityCheckThread)}");
                CreateLatencySanityCheckThread();
            }
        })
        {
            Name = $"{nameof(DiscordService)}.{nameof(LatencySanityCheckThread)}"
        }.Start();
    }

    private void LatencySanityCheckThread()
    {
        Log.Info("Created thread");
        while (true)
        {
            if (_readyAt.HasValue && _latencyLastUpdated.HasValue)
            {
                if (_latencyLastUpdated.Value - _readyAt.Value < TimeSpan.FromMinutes(5))
                {
                    Thread.Sleep(60_000);
                    continue;
                }
                var now = DateTimeOffset.UtcNow;
                var delta = now > _latencyLastUpdated
                    ? now - _latencyLastUpdated
                    : _latencyLastUpdated - now;
                if (delta > TimeSpan.FromMinutes(5))
                {
                    Log.Fatal("Latency was last updated >5min ago!!! Aborting process so it can be automatically restarted by docker");
                    Environment.Exit(0);
                    return;
                }
            }

            Thread.Sleep(1_000);
        }
    }

    public async Task Run()
    {
        await _client.LoginAsync(TokenType.Bot, _configData.DiscordToken);
        await _client.StartAsync();
    }

    #region Event Emit
    public event DiscordControllerDelegate? Ready;
    public bool IsReady { get; private set; }
    private async Task InvokeReady()
    {
        _readyAt = DateTimeOffset.UtcNow;
        if (Ready != null && !IsReady)
        {
            IsReady = true;
            Ready?.Invoke(this);
        }
        if (_interactionHandler != null)
            await _interactionHandler.InitializeAsync();
        var versionString = "v0.0";
        if (_details.VersionRaw != null)
        {
            versionString = $"v{_details.VersionRaw.Major}.{_details.VersionRaw.Minor}";
        }

        if (_details.Platform == XeniaPlatform.Bot)
        {
            await _client.SetGameAsync($"{versionString} | xenia.kate.pet", null);
        }
        Log.Info("Bot is ready!");
    }

    public event Func<SocketMessage, Task>? MessageReceived;
    #endregion

    #region Event Handling
    private static Task DiscordClientLogHandler(LogMessage arg)
    {
        var discordLog = LogManager.LogFactory.GetLogger("Discord" + (string.IsNullOrEmpty(arg.Source) ? "" : "." + arg.Source));
        switch (arg.Severity)
        {
            case LogSeverity.Debug:
            case LogSeverity.Verbose:
                discordLog.Debug(arg.Exception, arg.Message);
                break;
            case LogSeverity.Info:
                discordLog.Info(arg.Exception, arg.Message);
                break;
            case LogSeverity.Warning:
                discordLog.Warn(arg.Exception, arg.Message);
                break;
            case LogSeverity.Error:
                discordLog.Error(arg.Exception, arg.Message);
                break;
            case LogSeverity.Critical:
                discordLog.Fatal(arg.Exception, arg.Message);
                break;
        }
        return Task.CompletedTask;
    }
    #endregion
}
