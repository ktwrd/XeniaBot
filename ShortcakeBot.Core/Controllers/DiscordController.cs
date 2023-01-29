using Discord;
using Discord.WebSocket;
using kate.shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using ShortcakeBot.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ShortcakeBot.Core.Controllers
{
    public class DiscordController
    {
        private readonly IServiceProvider _services;
        private readonly DiscordSocketClient _client;
        private readonly ConfigManager.Config _config;
        private readonly CommandHandler _commandHandler;
        private readonly InteractionHandler _interactionHandler;
        public DiscordController(IServiceProvider services)
        {
            _config = services.GetRequiredService<ConfigManager.Config>();
            _client = services.GetRequiredService<DiscordSocketClient>();

            _commandHandler = services.GetRequiredService<CommandHandler>();
            _interactionHandler = services.GetRequiredService<InteractionHandler>();

            _services = services;

            _client.Log += _client_Log;
            _client.Ready += _client_Ready;
        }

        public async Task Run()
        {
            _client.MessageReceived += (arg) =>
            {
                MessageRecieved?.Invoke(arg);
                return Task.CompletedTask;
            };

            await _client.LoginAsync(TokenType.Bot, _config.DiscordToken);
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

        public event Func<SocketMessage, Task> MessageRecieved;
        #endregion

        #region Event Handling
        private async Task _client_Ready()
        {
            InvokeReady();
            await _commandHandler.InitializeAsync();
            await _interactionHandler.InitializeAsync();
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
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildMembers,
                UseInteractionSnowflakeDate = false,
                HandlerTimeout = 6000
            };
        }
    }
}
