using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using XeniaBot.Shared.Helpers;
using XeniaDiscord.Common.Services;
using XeniaDiscord.Data;

namespace XeniaDiscord.Shared.Interactions.Modules;

[Group("confessadmin", "Administrative tools for the Confession Module")]
public class ConfessionAdminModule : InteractionModuleBase
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly ApplicationDbContext _db;
    private readonly IConfessionService _confessionService;
    public ConfessionAdminModule(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _confessionService = services.GetRequiredService<IConfessionService>();
    }

    [RequireUserPermission(ChannelPermission.ManageChannels)]
    [SlashCommand("set-channel", "Set the output channel for confessions.")]
    public async Task SetOutputChannel(ITextChannel channel)
    {
        await DeferAsync();
        await Task.Delay(50);
        var guild = await Context.Client.GetGuildAsync(Context.Interaction.GuildId.GetValueOrDefault(0));
        if (guild == null)
        {
            await FollowupAsync("This command can only be ran in guilds.", ephemeral: true);
            return;
        }

        try
        {
            var embed = await _confessionService.SetOutputChannelAsync(guild, channel, Context.Interaction.User);
            await FollowupAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to set modal message for channel {channel.Name} ({channel.Id})");
            var embed = new EmbedBuilder()
                .WithTitle("Confession Admin - Error")
                .WithDescription($"Fatal Error!\n```\n{ex.GetType().Name}: {ex.Message[..Math.Max(1500, ex.Message.Length)]}\n```")
                .WithColor(Color.Red)
                .WithCurrentTimestamp();
            if (SentrySdk.IsEnabled)
            {
                var id = SentrySdk.CaptureException(ex, scope =>
                {
                    scope.SetInteractionInfo(Context);

                    scope.SetExtra("param.channel.id", channel.Id);
                    scope.SetExtra("param.channel.name", channel.Name);
                    scope.SetExtra("param.channel._type", channel.GetType());
                });
                embed.WithFooter(id.ToString());
            }
            await FollowupAsync(embed: embed.Build(), ephemeral: true);
        }
    }

    [SlashCommand("send-modal-message", "Send a message in a channel that has a button on it to create a confession.")]
    [RequireUserPermission(ChannelPermission.ManageChannels)]
    public async Task SendModalMessage(
        ITextChannel channel)
    {
        await DeferAsync();
        var guild = await Context.Client.GetGuildAsync(Context.Interaction.GuildId.GetValueOrDefault(0));
        if (guild == null)
        {
            await FollowupAsync("This command can only be ran in guilds.", ephemeral: true);
            return;
        }

        try
        {
            var config = await _confessionService.GetOrCreateGuildConfig(guild, Context.Interaction.User);
            var msg = await _confessionService.SendModalMessage(config, guild, channel);

            await FollowupAsync(embed: new EmbedBuilder()
                .WithTitle("Confession Admin - Send Modal Message")
                .WithDescription($"Successfully sent confession modal message.\n[Jump to message]({msg.GetJumpUrl()})")
                .Build());
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to set modal message for channel {channel.Name} ({channel.Id})");
            var embed = new EmbedBuilder()
                .WithTitle("Confession Admin - Error")
                .WithDescription($"Fatal Error!\n```\n{ex.GetType().Name}: {ex.Message[..Math.Max(1500, ex.Message.Length)]}\n```")
                .WithColor(Color.Red)
                .WithCurrentTimestamp();
            if (SentrySdk.IsEnabled)
            {
                var id = SentrySdk.CaptureException(ex, scope =>
                {
                    scope.SetInteractionInfo(Context);

                    scope.SetExtra("param.channel.id", channel.Id);
                    scope.SetExtra("param.channel.name", channel.Name);
                    scope.SetExtra("param.channel._type", channel.GetType());
                });
                embed.WithFooter(id.ToString());
            }
            await FollowupAsync(embed: embed.Build(), ephemeral: true);
        }
    }
}
