using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Shared;

namespace XeniaBot.Data
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
        }
        public async Task InitializeAsync()
        {
            var mods = Array.Empty<ModuleInfo>();
            foreach (var item in AppDomain.CurrentDomain.GetAssemblies())
            {
                mods = mods.Concat(_interactionService.AddModulesAsync(item, _services).Result).ToArray();
            }
            await _interactionService.RegisterCommandsGloballyAsync();

            var lines = new List<string>();
            foreach (var item in mods)
            {
                int count = 0;
                count += item.AutocompleteCommands.Count;
                count += item.ComponentCommands.Count;
                count += item.ContextCommands.Count;
                count += item.ModalCommands.Count;
                count += item.SlashCommands.Count;
                lines.Add($"- {item.Name} ({count})");
            }
            Log.Debug($"Loaded [{mods.Count()}] modules\n" + string.Join("\n", lines));
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
