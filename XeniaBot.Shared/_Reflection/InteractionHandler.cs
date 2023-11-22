using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Discord.Rest;
using XeniaBot.Shared;

namespace XeniaBot.Shared
{
    public class InteractionHandler
    {
        private readonly InteractionService _interactionService;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private readonly ConfigData _configData;
        public InteractionHandler(IServiceProvider services)
        {
            _interactionService = services.GetRequiredService<InteractionService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _configData = services.GetRequiredService<ConfigData>();
            _services = services;
        }

        public async Task PostDBL(IReadOnlyCollection<RestGlobalCommand> data)
        {
            try
            {
                if (string.IsNullOrEmpty(_configData.DiscordBotList_Token))
                {
                    Log.WriteLine($"Ignoring since DiscordBotList_Token is empty");
                    return;
                }
                var blacklist = new Dictionary<string, string[]>()
                {
                    {
                        "bansync", new string[]
                        {
                            "enableguild", "setguildstate"
                        }
                    },
                    {
                        "auth", Array.Empty<string>()
                    },
                    {
                        "fetch_config", Array.Empty<string>()
                    },
                    {
                        "metricreload", Array.Empty<string>()
                    }
                };
                var casted = new List<DiscordApplicationCommand>();
                foreach (var item in data)
                    if (!blacklist.ContainsKey(item.Name))
                        casted.Add(new DiscordApplicationCommand().Cast(item, blacklist));

                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri($"https://discordbotlist.com/api/v1/bots/{_client.CurrentUser.Id}/commands"),
                    Method = HttpMethod.Post,
                };
                request.Headers.Add("Authorization", $"Bot {_configData.DiscordBotList_Token}");
                var ser = JsonSerializer.Serialize(
                    casted, new JsonSerializerOptions()
                    {
                        IncludeFields = true,
                        IgnoreReadOnlyFields = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        ReferenceHandler = ReferenceHandler.Preserve
                    });
                request.Content = new StringContent(ser, null, "application/json");
                var client = new HttpClient();
                var res = await client.SendAsync(request);
                Log.Debug($"DBL Response: {res.StatusCode}");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to send commands to DiscordBotList.com\n{ex}");
            }
        }
        public async Task InitializeAsync()
        {
            var mods = Array.Empty<ModuleInfo>();
            foreach (var item in AppDomain.CurrentDomain.GetAssemblies())
            {
                mods = mods.Concat(_interactionService.AddModulesAsync(item, _services).Result).ToArray();
            }
            var result = await _interactionService.RegisterCommandsGloballyAsync();
            await PostDBL(result);
            
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
