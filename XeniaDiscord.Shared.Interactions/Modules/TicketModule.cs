using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using XeniaBot.Shared;
using XeniaDiscord.Common.Interfaces;
using XeniaDiscord.Data.Models.Ticket;

namespace XeniaDiscord.Shared.Interactions.Modules;

[Group("ticket", "Ticketing Module")]
public class TicketModule : InteractionModuleBase
{
    private readonly ITicketService _ticketService;
    public TicketModule(IServiceProvider services)
    {
        _ticketService = services.GetRequiredService<ITicketService>();
    }
    [SlashCommand("create", "Create a new ticket")]
    public async Task CreateTicket()
    {
        var baseEmbed = new EmbedBuilder();
        baseEmbed.Timestamp = DateTimeOffset.UtcNow;
        baseEmbed.WithFooter("Xenia Ticket Management");

        GuildTicketModel? model = null;
        try
        {
            model = await _ticketService.CreateTicket(Context.Guild.Id);
            if (model == null)
                throw new TicketException("Got null ticket details from controller");

            await _ticketService.UserAccessGrant(model.GetChannelId(), Context.User.Id);
        }
        catch (TicketException exception)
        {
            baseEmbed.Title = "Failed to Create Ticket";
            baseEmbed.Description = FormatException(exception);
            baseEmbed.Color = new Color(255, 255, 0);
        }
        catch (Exception exception)
        {
            baseEmbed.Title = "Failed to Create Ticket";
            baseEmbed.Description = FormatException(exception);
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

        await Context.Interaction.RespondAsync(embed: baseEmbed.Build());
    }

    [RequireUserPermission(GuildPermission.ManageChannels)]
    [SlashCommand("resolve", "Mark ticket as resolved")]
    public async Task ResolveTicket(
        [Discord.Interactions.Summary(description: "Channel of the ticket to resolve. Will assume current channel if not provided.")]
        [ChannelTypes(ChannelType.Text)] IChannel? ticketChannel = null)
    {
        // When ticket channel is null, assume we're talking about the current channel.
        if (ticketChannel == null)
            ticketChannel = Context.Channel;

        var embed = new EmbedBuilder()
        {
            Title = "Resolved Ticket",
            Description = $"Resolved ticket at <#{ticketChannel.Id}>",
            Timestamp = DateTimeOffset.UtcNow
        };
        try
        {
            await _ticketService.CloseTicket(ticketChannel.Id, GuildTicketStatus.Resolved, Context.User.Id);
        }
        catch (TicketException exception)
        {
            embed.Title = "Failed to Close Ticket";
            embed.Description = FormatException(exception);
            embed.Color = new Color(255, 255, 0);
        }
        catch (Exception exception)
        {
            embed.Title = "Failed to Close Ticket";
            embed.Description = FormatException(exception);
            embed.Color = Color.Red;
        }

        await Context.User.SendMessageAsync(embed: embed.Build());

        await Context.Interaction.RespondAsync(embed: embed.Build());
    }
    [RequireUserPermission(GuildPermission.ManageChannels)]
    [SlashCommand("reject", "Mark ticket as rejected")]
    public async Task RejectTicket(
        [Discord.Interactions.Summary(description: "Channel of the ticket to reject. Will assume current channel if not provided.")]
        [ChannelTypes(ChannelType.Text)] IChannel ticketChannel = null)
    {
        // When ticket channel is null, assume we're talking about the current channel.
        if (ticketChannel == null)
            ticketChannel = Context.Channel;

        var embed = new EmbedBuilder()
        {
            Title = "Rejected Ticket",
            Description = $"Rejected ticket at <#{ticketChannel.Id}>",
            Timestamp = DateTimeOffset.UtcNow
        };
        try
        {
            await _ticketService.CloseTicket(ticketChannel.Id, GuildTicketStatus.Rejected, Context.User.Id);
        }
        catch (TicketException exception)
        {
            embed.Title = "Failed to Close Ticket";
            embed.Description = FormatException(exception);
            embed.Color = new Color(255, 255, 0);
        }
        catch (Exception exception)
        {
            embed.Title = "Failed to Close Ticket";
            embed.Description = FormatException(exception);
            embed.Color = Color.Red;
        }

        await Context.User.SendMessageAsync(embed: embed.Build());

        await Context.Interaction.RespondAsync(embed: embed.Build());
    }

    private string FormatException(Exception error)
    {
        if (!string.IsNullOrEmpty(error.StackTrace))
        {
            var sb = new StringBuilder();
            sb.Append(error.Message);
            sb.Append("\n\n");
            sb.Append("```\n");
            sb.Append(error.StackTrace.Substring(0, Math.Min(3000, error.StackTrace.Length)));
            sb.Append("\n```");
            return sb.ToString();
        }
        return error.Message;
    }
}
