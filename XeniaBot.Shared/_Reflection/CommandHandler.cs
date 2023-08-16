using Discord.Commands;
using Discord.WebSocket;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.Data
{
    public class CommandHandler
    {
        // setup fields to be set later in the constructor
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private readonly ConfigData _configData;

        public CommandHandler(IServiceProvider services)
        {
            _configData = services.GetRequiredService<ConfigData>();
            // juice up the fields with these services
            // since we passed the services in, we can use GetRequiredService to pass them into the fields set earlier
            _commands = services.GetRequiredService<CommandService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            // take action when we execute a command
            _commands.CommandExecuted += CommandExecutedAsync;

            // take action when we receive a message (so we can process it, and see if it is a valid command)
            _client.MessageReceived += MessageReceivedAsync;
        }

        public async Task InitializeAsync()
        {
            // register modules that are public and inherit ModuleBase<T>.
            var modinfo = Array.Empty<ModuleInfo>();
            foreach (var item in AppDomain.CurrentDomain.GetAssemblies())
            {
                modinfo = modinfo.Concat(_commands.AddModulesAsync(item, _services).Result).ToArray();
            }
            Log.Debug($"Loaded {modinfo.Count()} modules");
        }

        // this class is where the magic starts, and takes actions upon receiving messages
        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            // ensures we don't process system/other bot messages
            if (!(rawMessage is SocketUserMessage message))
            {
                return;
            }

            if (message.Source != MessageSource.User)
            {
                return;
            }


            // sets the argument position away from the prefix we set
            var argPos = 0;

            var context = new SocketCommandContext(_client, message);
            if (_configData.DeveloperMode)
            {
                if (context.Guild?.Id != _configData.DeveloperMode_Server)
                    return;
            }

            if (_configData.UserWhitelistEnable)
            {
                if (!_configData.UserWhitelist.Contains(context.User.Id))
                    return;
            }

            // determine if the message has a valid prefix, and adjust argPos based on prefix
            if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasStringPrefix(XeniaHelper.GetGuildPrefix(context.Guild.Id, _configData), ref argPos)))
            {
                return;
            }

            // execute command if one is found that matches
            await _commands.ExecuteAsync(context, argPos, _services);
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, Discord.Commands.IResult result)
        {
            // if a command isn't found, log that info to console and exit this method
            if (!command.IsSpecified)
            {
                Log.Error($"Command failed to execute for [{context.User.Username}] <-> [{result.ErrorReason}]!");
                return;
            }


            // log success to the console and exit this method
            if (result.IsSuccess)
            {
                Log.Debug($"Command [{command.Value.Name}] executed for [{context.User.Username}] on [{context.Guild.Name}]");
                return;
            }

            // failure scenario, let's let the user know
            await context.Channel.SendMessageAsync($"Sorry, {context.User.Username}... something went wrong -> [{result}]!");
        }
    }
}
