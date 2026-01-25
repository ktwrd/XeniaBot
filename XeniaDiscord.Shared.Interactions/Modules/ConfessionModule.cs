using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using XeniaBot.Shared.Helpers;
using XeniaDiscord.Common;
using XeniaDiscord.Common.Interfaces;

namespace XeniaDiscord.Shared.Interactions.Modules;

[CommandContextType(InteractionContextType.Guild)]
public class ConfessionModule : InteractionModuleBase
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly IConfessionService _confessionService;
    public ConfessionModule(IServiceProvider services)
    {
        _confessionService = services.GetRequiredService<IConfessionService>();
    }

    [SlashCommand("confess", "Make a confession.")]
    public async Task CreateModalCommand()
    {
        IGuild? guild = null;
        try
        {
            guild = await Context.Client.GetGuildAsync(Context.Interaction.GuildId.GetValueOrDefault(0));
            if (guild == null)
            {
                await RespondAsync("This command can only be ran in guilds.", ephemeral: true);
                return;
            }
            var guildRecord = await _confessionService.GetOrCreateGuildConfig(guild, Context.Interaction.User);
            if (guildRecord == null)
            {
                await RespondAsync(embed: new EmbedBuilder()
                        .WithTitle("Create Confession - Error")
                        .WithDescription($"This module has not been setup on this server.")
                        .WithColor(Color.Red).Build(),
                    ephemeral: true);
                return;
            }

            var outputChannelId = guildRecord.GetOutputChannelId();
            if (outputChannelId == null || outputChannelId < 10)
            {
                await RespondAsync(embed: new EmbedBuilder()
                        .WithTitle("Create Confession - Error")
                        .WithDescription($"This module has not been setup **properly** on this server. (invalid output channel)")
                        .WithColor(Color.Red).Build(),
                    ephemeral: true);
                return;
            }
            var outputChannel = await guild.GetTextChannelAsync(outputChannelId.Value);
            if (outputChannel == null)
            {
                await RespondAsync(embed: new EmbedBuilder()
                    .WithTitle("Create Confession - Error")
                    .WithDescription($"Could not find output channel `{outputChannelId.Value}`")
                    .WithColor(Color.Red).Build(), ephemeral: true);
                return;
            }
            await RespondWithModalAsync<ConfessionModal>(InteractionIdentifier.ConfessionModal);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to create confession via command.");
            await RespondAsync(embed: new EmbedBuilder()
                .WithTitle("Create Confession - Error")
                .WithDescription($"Fatal Error!\n```\n{ex.GetType().Name}: {ex.Message}\n```")
                .WithColor(Color.Red)
                .WithCurrentTimestamp()
                .Build(), ephemeral: true);
            if (SentrySdk.IsEnabled)
            {
                SentrySdk.CaptureException(ex, scope =>
                {
                    scope.SetInteractionInfo(Context);
                });
            }
        }
    }

    [ComponentInteraction(InteractionIdentifier.ConfessionModalCreate)]
    public async Task CreateModalInteraction()
    {
        try
        {
            await RespondWithModalAsync<ConfessionModal>(InteractionIdentifier.ConfessionModal);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to execute interaction");
            var embed = new EmbedBuilder()
                .WithTitle("Failed to execute interaction.")
                .WithDescription(ex.Message[..Math.Min(1900, ex.Message.Length)])
                .WithColor(Color.Red)
                .WithCurrentTimestamp();
            if (SentrySdk.IsEnabled)
            {
                var id = SentrySdk.CaptureException(ex, scope =>
                {
                    scope.SetInteractionInfo(Context);
                });
                embed.WithFooter(id.ToString());
            }
            try
            {
                await RespondAsync(embed: embed.Build());
            }
            catch (Exception iex)
            {
                _log.Error(iex, "How the fuck did an exception get thrown here????");
            }
        }
    }

    [ModalInteraction(InteractionIdentifier.ConfessionModal)]
    public async Task HandleConfessionModal(ConfessionModal modal)
    {
        await DeferAsync();
        var guild = await Context.Client.GetGuildAsync(Context.Interaction.GuildId.GetValueOrDefault(0));
        if (guild == null)
        {
            await FollowupAsync("This interaction can only be ran in guilds/servers.", ephemeral: true);
            return;
        }
        try
        {
            var result = await _confessionService.CreateAsync(guild, Context.User, modal.Content);
            await FollowupAsync(embed: result.Build(), ephemeral: true);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to execute interaction");
            var embed = new EmbedBuilder()
                .WithTitle("Failed to execute interaction.")
                .WithDescription(ex.Message.Substring(0, Math.Min(1900, ex.Message.Length)))
                .WithColor(Color.Red)
                .WithCurrentTimestamp();
            if (SentrySdk.IsEnabled)
            {
                var id = SentrySdk.CaptureException(ex, scope =>
                {
                    scope.SetInteractionInfo(Context);
                });
                embed.WithFooter(id.ToString());
            }
            try
            {
                await RespondAsync(embed: embed.Build());
            }
            catch (Exception iex)
            {
                _log.Error(iex, "How the fuck did an exception get thrown here????");
            }
        }
    }
}

public class ConfessionModal : IModal
{
    public string Title => "Confess";

    [InputLabel("Content")]
    [ModalTextInput(InteractionIdentifier.ConfessionModalContent, TextInputStyle.Paragraph, maxLength: 2000)]
    public string Content { get; set; } = "";
}