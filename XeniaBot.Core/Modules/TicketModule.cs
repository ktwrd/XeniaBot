using Discord;
using Discord.Interactions;
using XeniaBot.Core.Services.BotAdditions;
using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using XeniaBot.MongoData.Models;
using XeniaBot.Shared;

namespace XeniaBot.Core.Modules;

[Group("ticket", "Ticketing Module")]
[CommandContextType(InteractionContextType.Guild)]
[RequireBotPermission(GuildPermission.ManageChannels)]
public class TicketModule : InteractionModuleBase
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly TicketService _ticketService;
    public TicketModule(IServiceProvider services)
    {
        _ticketService = services.GetRequiredService<TicketService>();
    }
    
    [UsedImplicitly]
    [SlashCommand("create", "Create a new ticket")]
    public async Task CreateTicket()
    {
        await DeferAsync();
        var baseEmbed = new EmbedBuilder()
            .WithCurrentTimestamp()
            .WithFooter("Xenia Ticket Management");

        TicketModel? model = null;
        try
        {
            model = await _ticketService.CreateTicket(Context.Guild.Id);
            if (model == null)
                throw new TicketException("Internal error (got no Ticket Details when creating one)");

            await _ticketService.UserAccessGrant(model.ChannelId, Context.User.Id);
        }
        catch (TicketException ex)
        {
            _log.Error(ex, $"Failed to create Ticket (guild: {Context.Guild.Id}, user: {Context.User.Username},{Context.User.Id})");
            baseEmbed.Title = "Failed to Create Ticket";
            baseEmbed.Description = FormatException(ex);
            baseEmbed.Color = new Color(255, 255, 0);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to create Ticket (guild: {Context.Guild.Id}, user: {Context.User.Username},{Context.User.Id})");
            baseEmbed.Title = "Failed to Create Ticket";
            baseEmbed.Description = FormatException(ex);
            baseEmbed.Color = Color.Red;
        }

        if (model == null)
        {
            baseEmbed.Title = "Failed to Create Ticket";
            baseEmbed.Description = "Got null ticket details from server";
            baseEmbed.Color = Color.Red;
        }
        else
        {
            baseEmbed.Title = "Ticket Created";
            baseEmbed.Description = $"Created <#{model.ChannelId}>. A staff member will aid you shortly.";
            baseEmbed.Color = Color.Green;
        }

        await FollowupAsync(embed: baseEmbed.Build());
    }

    [UsedImplicitly]
    [RequireUserPermission(GuildPermission.ManageChannels)]
    [SlashCommand("resolve", "Mark ticket as resolved")]
    public async Task ResolveTicket(
        [Summary(description: "Channel of the ticket to resolve. Will assume current channel if not provided.")]
        [ChannelTypes(ChannelType.Text)] IChannel? ticketChannel = null)
    {
        ticketChannel ??= Context.Channel;

        await DeferAsync();
        var embed = new EmbedBuilder()
        {
            Title = "Resolved Ticket",
            Description = $"Resolved ticket at <#{ticketChannel.Id}>",
            Timestamp = DateTimeOffset.UtcNow
        };
        try
        {
            await _ticketService.CloseTicket(ticketChannel.Id, TicketStatus.Resolved, Context.User.Id);
        }
        catch (TicketException ex)
        {
            if (ex.Message.StartsWith("Ticket Details not found"))
            {
                embed.Title = "Resolve Ticket - Error";
                embed.Description =
                    "Please provide a ticket, or run this command in a ticket channel that was made by Xenia.";
            }
            else
            {
                _log.Error(ex, $"Failed to create Ticket (channel: {ticketChannel?.Id}, guild: {Context.Guild.Id}, user: {Context.User.Username},{Context.User.Id})");
                embed.Title = "Failed to Resolve Ticket";
                embed.Description = FormatException(ex);
                embed.Color = new Color(255, 255, 0);
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to create Ticket (channel: {ticketChannel?.Id}, guild: {Context.Guild.Id}, user: {Context.User.Username},{Context.User.Id})");
            embed.Title = "Failed to Resolve Ticket";
            embed.Description = FormatException(ex);
            embed.Color = Color.Red;
        }

        try
        {
            await Context.User.SendMessageAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to notify user in DMs about ticket being resolved (ticketChannel: {ticketChannel?.Id}, guild: {Context.Guild.Id}, user: {Context.User.Username},{Context.User.Id})");
        }

        await FollowupAsync(embed: embed.Build());
    }
    
    [UsedImplicitly]
    [RequireUserPermission(GuildPermission.ManageChannels)]
    [SlashCommand("reject", "Mark ticket as rejected")]
    public async Task RejectTicket(
        [Summary(description: "Channel of the ticket to reject. Will assume current channel if not provided.")]
        [ChannelTypes(ChannelType.Text)] IChannel? ticketChannel = null)
    {
        // When ticket channel is null, assume we're talking about the current channel.
        ticketChannel ??= Context.Channel;

        var embed = new EmbedBuilder()
        {
            Title = "Rejected Ticket",
            Description = $"Rejected ticket at <#{ticketChannel.Id}>",
            Timestamp = DateTimeOffset.UtcNow
        };
        try
        {
            await _ticketService.CloseTicket(ticketChannel.Id, TicketStatus.Rejected, Context.User.Id);
        }
        catch (TicketException ex)
        {
            _log.Error(ex, $"Failed to reject Ticket (channel: {ticketChannel?.Id}, guild: {Context.Guild.Id}, user: {Context.User.Username},{Context.User.Id})");
            embed.Title = "Failed to Reject Ticket";
            embed.Description = FormatException(ex);
            embed.Color = new Color(255, 255, 0);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to reject Ticket (channel: {ticketChannel?.Id}, guild: {Context.Guild.Id}, user: {Context.User.Username},{Context.User.Id})");
            embed.Title = "Failed to Reject Ticket";
            embed.Description = FormatException(ex);
            embed.Color = Color.Red;
        }

        try
        {
            await Context.User.SendMessageAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to notify user in DMs about ticket being rejected (ticketChannel: {ticketChannel?.Id}, guild: {Context.Guild.Id}, user: {Context.User.Username},{Context.User.Id})");
        }

        await Context.Interaction.RespondAsync(embed: embed.Build());
    }

    private static string FormatException(Exception error)
    {
        var message = error.ToString().Trim();
        if (message.Length > 4096 - 3)
            message = message[..(4096 - 3)] + "...";
        return message;
    }
}
