using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Controllers.BotAdditions;
using XeniaBot.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Data.Models;

namespace XeniaBot.Core.Modules
{
    [Group("ticket", "Ticketing Module")]
    public class TicketModule : InteractionModuleBase
    {
        [SlashCommand("create", "Create a new ticket")]
        public async Task CreateTicket()
        {
            var controller = Program.Services.GetRequiredService<TicketController>();
            var baseEmbed = new EmbedBuilder();
            baseEmbed.Timestamp = DateTimeOffset.UtcNow;
            baseEmbed.WithFooter("Xenia Ticket Management");

            TicketModel model = null;
            try
            {
                model = await controller.CreateTicket(Context.Guild.Id);
                if (model == null)
                    throw new TicketException("Got null ticket details from controller");

                await controller.UserAccessGrant(model.ChannelId, Context.User.Id);
            }
            catch (TicketException exception)
            {
                baseEmbed.Title = "Failed to Create Ticket";
                baseEmbed.Description = $"{exception.Message}\n```\n{exception.StackTrace.Substring(0, Math.Min(3000, exception.StackTrace.Length))}\n```";
                baseEmbed.Color = new Color(255, 255, 0);
            }
            catch (Exception exception)
            {
                baseEmbed.Title = "Failed to Create Ticket";
                baseEmbed.Description = $"{exception.Message}\n```\n{exception.StackTrace.Substring(0, Math.Min(3000, exception.StackTrace.Length))}\n```";
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
            [ChannelTypes(ChannelType.Text)] IChannel ticketChannel = null)
        {
            // When ticket channel is null, assume we're talking about the current channel.
            if (ticketChannel == null)
                ticketChannel = Context.Channel;

            var controller = Program.Services.GetRequiredService<TicketController>();
            var embed = new EmbedBuilder()
            {
                Title = "Resolved Ticket",
                Description = $"Resolved ticket at <#{ticketChannel.Id}>",
                Timestamp = DateTimeOffset.UtcNow
            };
            try
            {
                await controller.CloseTicket(ticketChannel.Id, TicketStatus.Resolved, Context.User.Id);
            }
            catch (TicketException exception)
            {
                embed.Title = "Failed to Close Ticket";
                embed.Description = $"{exception.Message}\n```\n{exception.StackTrace.Substring(0, Math.Min(3000, exception.StackTrace.Length))}\n```";
                embed.Color = new Color(255, 255, 0);
            }
            catch (Exception exception)
            {
                embed.Title = "Failed to Close Ticket";
                embed.Description = $"{exception.Message}\n```\n{exception.StackTrace.Substring(0, Math.Min(3000, exception.StackTrace.Length))}\n```";
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

            var controller = Program.Services.GetRequiredService<TicketController>();
            var embed = new EmbedBuilder()
            {
                Title = "Rejected Ticket",
                Description = $"Rejected ticket at <#{ticketChannel.Id}>",
                Timestamp = DateTimeOffset.UtcNow
            };
            try
            {
                await controller.CloseTicket(ticketChannel.Id, TicketStatus.Rejected, Context.User.Id);
            }
            catch (TicketException exception)
            {
                embed.Title = "Failed to Close Ticket";
                embed.Description = $"{exception.Message}\n```\n{exception.StackTrace.Substring(0, Math.Min(3000, exception.StackTrace.Length))}\n```";
                embed.Color = new Color(255, 255, 0);
            }
            catch (Exception exception)
            {
                embed.Title = "Failed to Close Ticket";
                embed.Description = $"{exception.Message}\n```\n{exception.StackTrace.Substring(0, Math.Min(3000, exception.StackTrace.Length))}\n```";
                embed.Color = Color.Red;
            }

            await Context.User.SendMessageAsync(embed: embed.Build());

            await Context.Interaction.RespondAsync(embed: embed.Build());
        }
    }
}
