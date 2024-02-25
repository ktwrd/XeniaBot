using Discord;
using Discord.WebSocket;
using kate.shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Shared;
using Prometheus;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.Shared.Services;
[XeniaController]
public partial class DiscordService
{
    private readonly IServiceProvider _services;
    private readonly DiscordSocketClient _client;
    private readonly ConfigData _configData;
    private readonly CommandHandler? _commandHandler;
    private readonly InteractionHandler? _interactionHandler;
    private readonly PrometheusService _prom;
    private readonly ProgramDetails _details;
    public DiscordService(IServiceProvider services)
    {
        _details = services.GetRequiredService<ProgramDetails>();
        
        _configData = services.GetRequiredService<ConfigData>();
        _client = services.GetRequiredService<DiscordSocketClient>();

        if (_details.Platform == XeniaPlatform.Bot)
        {
            _commandHandler = services.GetRequiredService<CommandHandler>();
            _interactionHandler = services.GetRequiredService<InteractionHandler>();
        }
        else
        {
            _commandHandler = null;
            _interactionHandler = null;
        }

        _prom = services.GetRequiredService<PrometheusService>();

        _services = services;

        // Prometheus Events
        _prom.ServerStart += InitializeMetrics;
        _client.SlashCommandExecuted += Event_SlashCommandExecuted_Prom;

        _client.Log += _client_Log;
        _client.Ready += _client_Ready;
    }

    public async Task Run()
    {
        _client.MessageReceived += (arg) =>
        {
            MessageReceived?.Invoke(arg);
            return Task.CompletedTask;
        };

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

    public event Func<SocketMessage, Task> MessageReceived;
    #endregion

    #region Event Handling
    private async Task _client_Ready()
    {
        InvokeReady();
        if (_commandHandler != null)
            await _commandHandler.InitializeAsync();
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

    private Task _client_Log(LogMessage arg)
    {
        var methodName = arg.Source;
        var fileName = "Discord";
        if (methodName == "Discord")
            methodName = "";
        switch (arg.Severity)
        {
            case Discord.LogSeverity.Debug:
                Log.Debug(arg.Message, methodname: methodName, methodfile: fileName);
                break;
            case Discord.LogSeverity.Verbose:
                Log.Debug(arg.Message, methodname: methodName, methodfile: fileName);
                break;
            case Discord.LogSeverity.Info:
                Log.WriteLine(arg.Message, methodname: methodName, methodfile: fileName);
                break;
            case Discord.LogSeverity.Warning:
                Log.Warn(arg.Message, methodname: methodName, methodfile: fileName);
                break;
            case Discord.LogSeverity.Error:
                Log.Error(arg.Message, methodname: methodName, methodfile: fileName);
                break;
            case Discord.LogSeverity.Critical:
                Log.Error(arg.Message, methodname: methodName, methodfile: fileName);
                break;
        }
        if (arg.Exception != null)
            Log.Error(arg.Exception, methodname: methodName, methodfile: fileName);
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
