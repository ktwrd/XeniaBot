﻿using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using ShortcakeBot.Core.Controllers;
using ShortcakeBot.Core.Helpers;
using ShortcakeBot.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortcakeBot.Core.Modules
{
    [Discord.Interactions.Group("counter", "Configuration for the Counter module")]
    public class CounterModule : InteractionModuleBase
    {
        [SlashCommand("setchannel", "Set the channel for counting")]
        public async Task SetChannel(
            [ChannelTypes(ChannelType.Text)] IChannel targetChannel)
        {
            var guildUser = await Context.Guild.GetUserAsync(Context.User.Id);
            if (!guildUser.GuildPermissions.ManageChannels)
            {
                await Context.Interaction.RespondAsync("You do not have permission to execute this command. You need the `ManageChannels` permission");
                return;
            }

            CounterController controller = Program.Services.GetRequiredService<CounterController>();
            CounterGuildModel data = await controller.Get(Context.Guild);
            if (data == null)
            {
                data = new CounterGuildModel(targetChannel, Context.Guild);
                controller.Set(data);
            }
            else if (data != null && targetChannel.Id == data.ChannelId)
            {
                await Context.Interaction.RespondAsync($"<#{data.ChannelId}> is already this server's counting channel.");
                return;
            }

            data.ChannelId = targetChannel.Id;
            controller.Set(data);

            var guild = await Context.Client.GetGuildAsync(Context.Guild.Id);
            var targetTextChannel = await guild.GetTextChannelAsync(targetChannel.Id);
            await targetTextChannel.SendMessageAsync($"Counting has been enabled, start with `1`");

            await Context.Interaction.RespondAsync($"Counting channel changed to <#{data.ChannelId}>");
            return;
        }

        [SlashCommand("deletechannel", "Remove channel from storage")]
        public async Task DeleteChannel(
            [ChannelTypes(ChannelType.Text)] IChannel targetChannel)
        {
            var guildUser = await Context.Guild.GetUserAsync(Context.User.Id);
            if (!guildUser.GuildPermissions.ManageChannels)
            {
                await Context.Interaction.RespondAsync("You do not have permission to execute this command. You need the `ManageChannels` permission");
                return;
            }

            CounterController controller = Program.Services.GetRequiredService<CounterController>();
            CounterGuildModel data = controller.Get(targetChannel);
            if (data == null)
            {
                await Context.Interaction.RespondAsync($"Channel not found in database.");
                return;
            }

            try
            {
                await controller.Delete(targetChannel);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                DiscordHelper.ReportError(ex, Context);
                await Context.Interaction.RespondAsync($"Failed to delete record.\n```\n{ex.Message}\n```");
                return;
            }
            await Context.Interaction.RespondAsync($"Removed channel from database.");
        }
        [SlashCommand("delete", "Delete all Counting Channels in this server")]
        public async Task Delete()
        {
            var guildUser = await Context.Guild.GetUserAsync(Context.User.Id);
            if (!guildUser.GuildPermissions.ManageChannels)
            {
                await Context.Interaction.RespondAsync("You do not have permission to execute this command. You need the `ManageChannels` permission");
                return;
            }

            var controller = Program.Services.GetRequiredService<CounterController>();
            CounterGuildModel data = await controller.Get(Context.Guild);
            if (data == null)
            {
                await Context.Interaction.RespondAsync("Server not found in database");
                return;
            }

            try
            {
                await controller.Delete(Context.Guild);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                DiscordHelper.ReportError(ex, Context);
                await Context.Interaction.RespondAsync($"Failed to delete record.\n```\n{ex.Message}\n```");
                return;
            }
            await Context.Interaction.RespondAsync($"Removed server from database.");
        }

        [SlashCommand("info", "Information about the counter module for this guild")]
        public async Task Info()
        {
            var controller = Program.Services.GetRequiredService<CounterController>();
            var data = await controller.Get(Context.Guild);
            if (data == null)
            {
                await Context.Interaction.RespondAsync($"The Counter Module has not been setup. Use `/counter setchannel` to do that");
                return;
            }

            await Context.Interaction.RespondAsync($"<#{data.ChannelId}> is currently at `{data.Count}`");
        }
    }
}
