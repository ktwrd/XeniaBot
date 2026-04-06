using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared.Services;
using XeniaDiscord.Data.Models.ServerLog;
using XeniaDiscord.Data.Repositories;

namespace XeniaBot.Core.Modules;

[Group("log", "Configure Server Event Logging")]
[RequireUserPermission(GuildPermission.ManageGuild)]
[CommandContextType(InteractionContextType.Guild)]
public class ServerLogModule : InteractionModuleBase
{
    private readonly ErrorReportService _err;
    private readonly ServerLogRepository _repo;

    public ServerLogModule(IServiceProvider services)
    {
        _err = services.GetRequiredService<ErrorReportService>();
        _repo = services.GetRequiredService<ServerLogRepository>();
    }

    [SlashCommand("reset", "Reset server log configuration")]
    public async Task Reset()
    {
        await DeferAsync();
        try
        {
            var count = await _repo.RemoveEvents(Context.Guild.Id, Enum.GetValues<ServerLogEvent>());
            await FollowupAsync(embed: new EmbedBuilder()
            {
                Title = "Server Log - Reset Configuration",
                Description = $"Successfully reset configuration ({count} records deleted)",
                Color = Color.Blue
            }.Build());
        }
        catch (Exception ex)
        {
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes($"Failed to reset configuration")
                .WithContext(Context));
            await FollowupAsync(embed: new EmbedBuilder()
                {
                    Title = "Failed to reset configuration",
                    Description = $"Error has been reported ({ex.GetType().Namespace}.{ex.GetType().Name})",
                    Color = Color.Red
                }.Build());
        }
    }

    [SlashCommand("reset-channel", "Remove all events from a channel")]
    public async Task ResetChannel(
        [ChannelTypes(ChannelType.Text)] ITextChannel channel)
    {
        await DeferAsync();
        try
        {
            var count = await _repo.RemoveChannel(Context.Guild.Id, channel.Id);
            await FollowupAsync(embed: new EmbedBuilder()
            {
                Title = "Server Log - Reset Channel Configuration",
                Description = $"Successfully reset configuration for channel {channel.Mention} ({count} records deleted)",
                Color = Color.Blue
            }.Build());
        }
        catch (Exception ex)
        {
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes($"Failed to reset configuration")
                .WithContext(Context)
                .WithChannel(channel));
            await FollowupAsync(embed: new EmbedBuilder()
                {
                    Title = $"Failed to reset configuration for channel {channel.Mention}",
                    Description = $"Error has been reported ({ex.GetType().Namespace}.{ex.GetType().Name})",
                    Color = Color.Red
                }.Build());
        }
    }

    [SlashCommand("add-event", "Add an event to a channel")]
    public async Task AddChannelEvent(
        ServerLogEvent @event,
        [ChannelTypes(ChannelType.Text)] ITextChannel channel)
    {
        await DeferAsync();
        var embed = new EmbedBuilder()
            .WithTitle("Server Log - Add Channel Event")
            .WithColor(Color.Blue)
            .WithCurrentTimestamp();
        try
        {
            var currentMember = await Context.Guild.GetCurrentUserAsync();
            var ourChannelPermissions = currentMember.GetPermissions(channel);
            var missingPermissions = new List<string>();
            if (!ourChannelPermissions.ViewChannel)
            {
                missingPermissions.Add("View Channel");
            }
            if (!ourChannelPermissions.ReadMessageHistory)
            {
                missingPermissions.Add("Read Message History");
            }
            if (!ourChannelPermissions.SendMessages)
            {
                missingPermissions.Add("Send Messages");
            }
            if (!ourChannelPermissions.AttachFiles)
            {
                missingPermissions.Add("Attach Files");
            }
            if (!ourChannelPermissions.EmbedLinks)
            {
                missingPermissions.Add("Embed Links");
            }
            if (missingPermissions.Count == 0)
            {
                await _repo.AddChannelEvent(Context.Guild.Id, channel.Id, @event, Context.User);
                embed.WithDescription($"Successfully added event `{@event}` to channel {channel.Mention}");
            }
            else
            {
                embed.WithDescription(
                        string.Join("\n",
                        $"Missing permissions on channel {channel.Mention}",
                        "```",
                        string.Join("\n", missingPermissions),
                        "```"))
                    .WithFooter("Make sure that you directly give Xenia these permissions")
                    .WithColor(Color.Red);
            }
            await FollowupAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            var errorType = $"{ex.GetType().Name}.{ex.GetType().Name}".Replace("`", "\\`");
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes($"Failed to add event {@event} to channel {channel}")
                .WithContext(Context)
                .WithChannel(channel));
            
            await FollowupAsync(embed:
                embed.WithDescription(string.Join("\n", "Failed to add event to channel.", $"`{errorType}`"))
                .WithColor(Color.Red)
                .Build());
        }
    }

    [SlashCommand("get-channel-events", "See events being sent to a channel")]
    public async Task GetEventsByChannel(
        [ChannelTypes(ChannelType.Text)] ITextChannel channel)
    {
        await DeferAsync();
        var embed = new EmbedBuilder()
            .WithTitle("Server Log - Get Events by Channel")
            .WithColor(Color.Blue)
            .WithCurrentTimestamp();
        try
        {
            var items = await _repo.GetChannelsForGuild(Context.Guild.Id, channel.Id);
            var events = items.Select(e => e.Event).Distinct().ToList();
            if (events.Count < 1)
            {
                embed.WithDescription("No events.")
                .WithColor(Color.Orange);
            }
            else
            {
                embed.WithDescription("```\n" + string.Join("\n", events) + "\n```");
            }
            await FollowupAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            var errorType = $"{ex.GetType().Name}.{ex.GetType().Name}".Replace("`", "\\`");
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes($"Failed to get channel events")
                .WithContext(Context)
                .WithChannel(channel));
            
            await FollowupAsync(embed:
                embed.WithDescription(string.Join("\n", "Failed to get channel events.", $"`{errorType}`"))
                .WithColor(Color.Red)
                .Build());
        }
    }

    [SlashCommand("get-channels", "See channels that use an event")]
    public async Task GetChannelsByEvent(ServerLogEvent @event)
    {
        await DeferAsync();
        var embed = new EmbedBuilder()
            .WithTitle("Server Log - Get Channels by Event")
            .WithColor(Color.Blue)
            .WithCurrentTimestamp();
        try
        {
            var items = await _repo.GetChannelsForGuild(Context.Guild.Id, [@event]);
            var channelIds = items.Select(e => e.GetChannelId()).Distinct().ToList();
            if (channelIds.Count < 1)
            {
                embed.WithDescription($"No channels found for event `{@event}`")
                    .WithColor(Color.Orange);
            }
            else
            {
                embed.WithDescription(string.Join("\n", channelIds.Select(e => $"<#{e}>")));
            }
            await FollowupAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            var errorType = $"{ex.GetType().Name}.{ex.GetType().Name}".Replace("`", "\\`");
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes($"Failed to get channels for event {@event}")
                .WithContext(Context));
            
            await FollowupAsync(embed:
                embed.WithDescription(string.Join("\n", $"Failed to get channels by event `{@event}`.", $"`{errorType}`"))
                .WithColor(Color.Red)
                .Build());
        }
    }

    [SlashCommand("setchannel", "Set event channel")]
    public async Task SetChannel(
        ServerLogEvent logEvent,
        [ChannelTypes(ChannelType.Text)] ITextChannel channel)
    {
        await DeferAsync();
        try
        {
            
        }
        catch (Exception ex)
        {
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes($"Failed to set channel {channel} to use event {logEvent}")
                .WithContext(Context)
                .WithChannel(channel));
            await FollowupAsync(embed: new EmbedBuilder()
                {
                    Title = "Failed to set channel",
                    Description = $"Error has been reported ({ex.GetType().Namespace}.{ex.GetType().Name})",
                    Color = Color.Red
                }.Build());
        }
    }
    
    /*
     * TEMPLATe
    
    [SlashCommand("", "")]
    public async Task Channel([ChannelTypes(ChannelType.Text)] ITextChannel channel)
    {
        var res = await Logic((v) =>
        {
            return v;
        });
    }
     
     */
}