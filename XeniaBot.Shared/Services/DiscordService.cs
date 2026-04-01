using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Threading.Tasks;
using XeniaBot.Shared.Helpers;

using LogSeverity = Discord.LogSeverity;

namespace XeniaBot.Shared.Services;

[XeniaController]
public class DiscordService
{
    private static readonly Logger Log = LogManager.GetLogger("Xenia.DiscordService");
    private readonly DiscordSocketClient _client;
    private readonly ConfigData _configData;
    private readonly InteractionHandler? _interactionHandler;
    private readonly ProgramDetails _details;
    public DiscordService(IServiceProvider services)
    {
        _details = services.GetRequiredService<ProgramDetails>();

        _configData = services.GetRequiredService<ConfigData>();
        _client = services.GetRequiredService<DiscordSocketClient>();

        if (_details.Platform == XeniaPlatform.Bot)
        {
            _interactionHandler = services.GetRequiredService<InteractionHandler>();
        }
        else
        {
            _interactionHandler = null;
        }

        _client.Log += DiscordClientLogHandler;
        _client.Ready += OnClientReady;
        _client.MessageReceived += async (arg) =>
        {
            MessageReceived?.Invoke(arg);
        };
    }

    public async Task Run()
    {
        await _client.LoginAsync(TokenType.Bot, _configData.DiscordToken);
        await _client.StartAsync();
    }

    #region Event Emit
    public event DiscordControllerDelegate? Ready;
    public bool IsReady { get; private set; }
    private void InvokeReady()
    {
        if (Ready != null && !IsReady)
        {
            IsReady = true;
            Ready?.Invoke(this);
        }
    }

    public event Func<SocketMessage, Task>? MessageReceived;
    #endregion

    #region Event Handling
    private async Task OnClientReady()
    {
        InvokeReady();
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
    }

    private static Task DiscordClientLogHandler(LogMessage arg)
    {
        var discordLog = LogManager.GetLogger("Discord" + (string.IsNullOrEmpty(arg.Source) ? "" : "." + arg.Source));
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

    public static DiscordSocketConfig GetSocketClientConfig()
    {
        return new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.All,
            UseInteractionSnowflakeDate = false,
            AlwaysDownloadUsers = true
        };
    }
}
