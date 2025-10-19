using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using XeniaBot.Core.Helpers;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;

namespace XeniaDiscord.Shared.Interactions.Modules;

[Discord.Interactions.Group("log", "Configure Server Event Logging")]
[RequireUserPermission(GuildPermission.ManageGuild)]
public class ServerLogModule : InteractionModuleBase
{
    private async Task<EmbedBuilder> Logic(ServerLogEvent logEvent, ulong channelId)
    {
        try
        {
            var controller = Program.Core.GetRequiredService<ServerLogRepository>();

            var data = await controller.Get(Context.Guild.Id);
            data ??= new ServerLogModel()
            {
                ServerId = Context.Guild.Id,
                DefaultLogChannel = 0
            };
            
            data.SetChannel(logEvent, channelId);

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

    [SlashCommand("setchannel", "Set channel")]
    public async Task SetChannel(
        ServerLogEvent logEvent,
        [ChannelTypes(ChannelType.Text)] ITextChannel channel)
    {
        var res = await Logic(logEvent, channel.Id);
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