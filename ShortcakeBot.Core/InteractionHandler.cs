using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ShortcakeBot.Core
{
    public class InteractionHandler
    {
        private readonly InteractionService _interactionService;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        public InteractionHandler(IServiceProvider services)
        {
            _interactionService = services.GetRequiredService<InteractionService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            var mods = _interactionService.AddModulesAsync(Assembly.GetExecutingAssembly(), _services).Result;
            _interactionService.RegisterCommandsGloballyAsync().Wait();
            Log.Debug($"Loaded [{mods.Count()}] modules");

            _client.InteractionCreated += InteractionCreateAsync;
        }

        private async Task InteractionCreateAsync(SocketInteraction interaction)
        {
            var context = new SocketInteractionContext(
                _client,
                interaction);
            await _interactionService.ExecuteCommandAsync(
                context,
                _services);
        }
    }
}
