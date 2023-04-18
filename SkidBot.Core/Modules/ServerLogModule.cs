using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using SkidBot.Core.Controllers.BotAdditions;
using SkidBot.Core.Helpers;
using SkidBot.Core.Models;

namespace SkidBot.Core.Modules;

[Discord.Interactions.Group("log", "Configure Server Event Logging")]
[RequireUserPermission(GuildPermission.ManageGuild)]
public class ServerLogModule : InteractionModuleBase
{
    private async Task<EmbedBuilder> Logic(LogEvent logEvent, ulong channelId)
    {
        try
        {
            var controller = Program.Services.GetRequiredService<ServerLogConfigController>();

            var data = await controller.Get(Context.Guild.Id);
            switch (logEvent)
            {
                case LogEvent.Fallback:
                    data.DefaultLogChannel = channelId;
                    break;
                case LogEvent.Join:
                    data.JoinChannel = channelId;
                    break;
                case LogEvent.Leave:
                    data.LeaveChannel = channelId;
                    break;
                default:
                    throw new Exception($"LogEvent {logEvent} not implemented in database");
            }

            await controller.Set(data);
        }
        catch (Exception e)
        {
            await DiscordHelper.ReportError(e, Context);
            return new EmbedBuilder()
            {
                Title = "Failed to set channel",
                Description = $"```\n{e.Message}\n```",
                Color = Color.Red,
                Footer = new EmbedFooterBuilder()
                {
                    Text = "Developers have been notified"
                }
            };
        }

        return new EmbedBuilder()
        {
            Title = "Updated Config",
            Color = Color.Green
        };
    }
    public enum LogEvent
    {
        Fallback,
        Leave,
        Join,
        Ban,
        Kick
    }

    [SlashCommand("setchannel", "Set channel")]
    public async Task SetChannel(
        LogEvent logEvent, [ChannelTypes(ChannelType.Text)] ITextChannel channel)
    {
        //Console.WriteLine(JsonSerialize.Serialize(data.products["ENCHANTED_POTATO"].quick_status));
        var res = await Logic(logEvent);
        await Context.Interaction.RespondAsync(embed: res.Build());
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