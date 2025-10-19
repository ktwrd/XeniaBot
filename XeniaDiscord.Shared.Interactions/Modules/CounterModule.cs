using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Services.BotAdditions;
using XeniaBot.Core.Helpers;
using XeniaBot.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;

namespace XeniaDiscord.Shared.Interactions.Modules;

[Discord.Interactions.Group("counter", "Configuration for the Counter module")]
public class CounterModule : InteractionModuleBase
{
    [SlashCommand("setchannel", "Set the channel for counting")]
    [RequireUserPermission(ChannelPermission.ManageChannels)]
    public async Task SetChannel(
        [ChannelTypes(ChannelType.Text)] IChannel targetChannel)
    {
        var counterConfig = Program.Core.GetRequiredService<CounterConfigRepository>();
        var data = await counterConfig.Get(Context.Guild);
        if (data == null)
        {
            data = new CounterGuildModel(targetChannel, Context.Guild);
            await counterConfig.Set(data);
        }
        else if (targetChannel.Id == data.ChannelId)
        {
            await Context.Interaction.RespondAsync($"<#{data.ChannelId}> is already this server's counting channel.");
            return;
        }

        data.ChannelId = targetChannel.Id;
        await counterConfig.Set(data);

        var guild = await Context.Client.GetGuildAsync(Context.Guild.Id);
        var targetTextChannel = await guild.GetTextChannelAsync(targetChannel.Id);
        await targetTextChannel.SendMessageAsync("Counting has been enabled, start with `1`");

        await Context.Interaction.RespondAsync($"Counting channel changed to <#{data.ChannelId}>");
        return;
    }

    [SlashCommand("deletechannel", "Remove channel from storage")]
    [RequireUserPermission(ChannelPermission.ManageChannels)]
    public async Task DeleteChannel(
        [ChannelTypes(ChannelType.Text)] IChannel targetChannel)
    {
        var counterConfig = Program.Core.GetRequiredService<CounterConfigRepository>();
        var data = await counterConfig.Get(targetChannel);
        if (data == null)
        {
            await Context.Interaction.RespondAsync($"Channel not found in database.");
            return;
        }

        try
        {
            await counterConfig.Delete(targetChannel);
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
            var r = DiscordHelper.ReportError(ex, Context);
            await Context.Interaction.RespondAsync($"Failed to delete record.\n```\n{ex.Message}\n```");
            await r;
            return;
        }
        await Context.Interaction.RespondAsync($"Removed channel from database.");
    }
    [SlashCommand("delete", "Delete all Counting Channels in this server")]
    [RequireUserPermission(ChannelPermission.ManageChannels)]
    public async Task Delete()
    {
        var counterConfig = Program.Core.GetRequiredService<CounterConfigRepository>();
        var data = await counterConfig.Get(Context.Guild);
        if (data == null)
        {
            await Context.Interaction.RespondAsync("Server not found in database");
            return;
        }

        try
        {
            await counterConfig.Delete(Context.Guild);
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
            var r = DiscordHelper.ReportError(ex, Context);
            await Context.Interaction.RespondAsync($"Failed to delete record.\n```\n{ex.Message}\n```");
            await r;
            return;
        }
        await Context.Interaction.RespondAsync($"Removed server from database.");
    }

    [SlashCommand("info", "Information about the counter module for this guild")]
    public async Task Info()
    {
        var controller = Program.Core.GetRequiredService<CounterConfigRepository>();
        var data = await controller.Get(Context.Guild);
        if (data == null)
        {
            await Context.Interaction.RespondAsync($"The Counter Module has not been setup. Use `/counter setchannel` to do that");
            return;
        }

        await Context.Interaction.RespondAsync($"<#{data.ChannelId}> is currently at `{data.Count}`");
    }
}
