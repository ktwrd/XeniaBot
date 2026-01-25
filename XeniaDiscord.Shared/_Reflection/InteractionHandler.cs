using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Discord.Rest;
using Sentry;
using NLog;

namespace XeniaBot.Shared;

public class InteractionHandler
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
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
            if (string.IsNullOrEmpty(_configData.ApiKeys.DiscordBotList))
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
            foreach (var item in data.Where(e => !blacklist.ContainsKey(e.Name)))
                casted.Add(new DiscordApplicationCommand().Cast(item, blacklist));

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri($"https://discordbotlist.com/api/v1/bots/{_client.CurrentUser.Id}/commands"),
                Method = HttpMethod.Post,
            };
            request.Headers.Add("Authorization", $"Bot {_configData.ApiKeys.DiscordBotList}");
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
            _log.Debug($"DBL Response: {res.StatusCode}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to send commands to DiscordBotList.com");
        }
    }
    public async Task InitializeAsync()
    {
        var mods = Array.Empty<ModuleInfo>();
        foreach (var item in AppDomain.CurrentDomain.GetAssemblies())
        {
            var x = await _interactionService.AddModulesAsync(item, _services);
            mods = mods.Concat(x).ToArray();
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
        _log.Debug($"Loaded [{mods.Count()}] modules\n" + string.Join("\n", lines));
        _client.InteractionCreated += InteractionCreateAsync;
    }

    private async Task InteractionCreateAsync(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(
                _client,
                interaction);
            await _interactionService.ExecuteCommandAsync(
                context,
                _services);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to handle interaction {interaction.Id} in channel {interaction.Channel.Name} ({interaction.ChannelId}) from user {interaction.User} ({interaction.User.Id})");
            SentrySdk.CaptureException(ex);
        }
    }
}
