using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Sentry;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;

namespace XeniaBot.Shared;

public class InteractionHandler
{
    private static readonly Logger Log = LogManager.GetLogger("Xenia." + nameof(InteractionHandler));
    private readonly InteractionService _interactionService;
    private readonly DiscordShardedClient _client;
    private readonly CoreContext _coreContext;
    private readonly IServiceProvider _services;
    public InteractionHandler(IServiceProvider services)
    {
        _interactionService = services.GetRequiredService<InteractionService>();
        _client = services.GetRequiredService<DiscordShardedClient>();
        _coreContext = services.GetRequiredService<CoreContext>();
        _services = services;
    }

    public async Task InitializeAsync()
    {
        await _coreContext.RegisterModules(_interactionService, _services);
        await _interactionService.RegisterCommandsGloballyAsync(deleteMissing: true);
        if (_coreContext.RegisterDeveloperModules != null)
        {
            var devModules = await _coreContext.RegisterDeveloperModules(_interactionService, _services);
            await _interactionService.AddModulesToGuildAsync(SharedGlobals.InternalGuildId, true, devModules);
        }
        
        var lines = new List<string>();
        foreach (var item in _interactionService.Modules)
        {
            int count = 0;
            count += item.AutocompleteCommands.Count;
            count += item.ComponentCommands.Count;
            count += item.ContextCommands.Count;
            count += item.ModalCommands.Count;
            count += item.SlashCommands.Count;
            lines.Add($"- {item.Name} ({count})");
        }
        Log.Debug($"Loaded [{_interactionService.Modules.Count}] modules\n" + string.Join("\n", lines));
        _client.ModalSubmitted += ModalSubmittedAsync;
        _client.ButtonExecuted += ButtonExecutedAsync;
        _client.InteractionCreated += InteractionCreateAsync;
    }

    private async Task ButtonExecutedAsync(SocketMessageComponent interaction)
    {
        try
        {
            var context = new ShardedInteractionContext<SocketMessageComponent>(
                _client,
                interaction);
            var result = await _interactionService.ExecuteCommandAsync(
                context,
                _services);
            if (result.Error != null)
            {
                Log.Warn(result.ErrorReason);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to handle interation {interaction.Id} invoked by user \"{interaction.User.GlobalName}\" ({interaction.User.Username}, {interaction.User.Id})");
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetInteractionInfo(interaction);
            });
        }
    }

    private async Task ModalSubmittedAsync(SocketModal interaction)
    {
        try
        {
            var context = new ShardedInteractionContext<SocketModal>(
                _client,
                interaction);
            var result = await _interactionService.ExecuteCommandAsync(
                context,
                _services);
            if (result.Error != null)
            {
                Log.Warn(result.ErrorReason);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to handle interation {interaction.Id} invoked by user \"{interaction.User.GlobalName}\" ({interaction.User.Username}, {interaction.User.Id})");
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetInteractionInfo(interaction);
            });
        }
    }

    private async Task InteractionCreateAsync(SocketInteraction interaction)
    {
        try
        {
            Log.Trace(FormatName(interaction));
            var context = new ShardedInteractionContext(
                _client,
                interaction);
            var result = await _interactionService.ExecuteCommandAsync(
                context,
                _services);
            if (!result.IsSuccess)
            {
                Log.Warn(result.ErrorReason);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to handle interation {interaction.Id} invoked by user \"{interaction.User.GlobalName}\" ({interaction.User.Username}, {interaction.User.Id})");
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetInteractionInfo(interaction);
            });
        }
    }

    private static string FormatName(IDiscordInteraction interaction)
    {
        var sb = new StringBuilder(interaction.Id.ToString());
        if (interaction.Data is SocketMessageComponentData messageComponentData)
        {
            sb.Append(" - ");
            sb.Append(messageComponentData.CustomId);
        }

        return sb.ToString();
    }
}
