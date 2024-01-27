﻿using Discord.Commands;
using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaBot.Shared.Controllers;

namespace XeniaBot.Core.Helpers
{
    public static class DiscordHelper
    {
        public static EmbedBuilder BaseEmbed(EmbedBuilder? embed=null)
        {
            if (embed == null)
                embed = new EmbedBuilder();

            var client = Program.Core.GetRequiredService<DiscordSocketClient>();
            var icon = client.CurrentUser.GetAvatarUrl();

            return embed
                .WithTimestamp(DateTimeOffset.UtcNow)
                .WithFooter(new EmbedFooterBuilder()
                    .WithText($"Xenia v{Program.Version}")
                    .WithIconUrl(icon));
        }
        public static async Task DeleteMessage(DiscordSocketClient client, SocketMessage argu)
        {
            if (!(argu is SocketUserMessage message))
                return;
            var context = new SocketCommandContext(client, message);
            var guild = context.Guild.GetTextChannel(argu.Channel.Id);
            var msg = await guild.GetMessageAsync(argu.Id);

            if (msg != null)
                await msg.DeleteAsync();
        }
        public static string GetUptimeString()
        {
            var current = DateTimeOffset.UtcNow;
            var start = DateTimeOffset.FromUnixTimeSeconds(Program.StartTimestamp);
            var diff = current - start;
            var data = new List<string>();
            if (Math.Floor(diff.TotalHours) > 0)
                data.Add($"{Math.Floor(diff.TotalHours)}hr");
            if (diff.Minutes > 0)
                data.Add($"{diff.Minutes}m");
            if (diff.Seconds > 0)
                data.Add($"{diff.Seconds}s");
            return string.Join(" ", data);
        }

        #region HasGuildPermission
        public static async Task<bool> HasGuildPermission(IGuild guild, IUser user, GuildPermission[] permissions)
        {
            var guildUser = await guild.GetUserAsync(user.Id);
            foreach (var item in permissions)
                if (guildUser.GuildPermissions.Has(item))
                    return true;
            return false;
        }
        public static async Task<bool> HasGuildPermission(IGuild guild, IUser user, GuildPermission permission)
        {
            return await HasGuildPermission(guild, user, new GuildPermission[] { permission });
        }
        public static async Task<bool> HasGuildPermission(IInteractionContext context, GuildPermission[] permissions, bool sendReply = false)
        {
            var missingPermissions = new List<GuildPermission>();
            var guildUser = await context.Guild.GetUserAsync(context.User.Id);
            foreach (var item in permissions)
                if (!guildUser.GuildPermissions.Has(item))
                    missingPermissions.Add(item);

            if (missingPermissions.Count > 0)
            {
                if (sendReply)
                {
                    var missingPermissionsContent = string.Join("\n", missingPermissions.Select(v => v.ToString()));
                    await context.Interaction.RespondAsync($"You do not have permission to execute this command. You require the following permissions\n```\n{missingPermissionsContent}\n```", ephemeral: true);
                }
                return false;
            }
            else
            {
                return true;
            }
        }
        public static async Task<bool> HasGuildPermission(IInteractionContext context, GuildPermission permission, bool sendReply = false)
        {
            return await HasGuildPermission(context, new GuildPermission[] { permission }, sendReply);
        }
        #endregion

        #region Error Reporting
        public static async Task ReportError(HttpResponseMessage response, ICommandContext commandContext)
        {
            await ReportError(response,
                commandContext.User,
                commandContext.Guild,
                commandContext.Channel,
                commandContext.Message);
        }
        public static async Task ReportError(HttpResponseMessage response, IInteractionContext context)
        {
            await ReportError(response,
                context.User,
                context.Guild,
                context.Channel,
                null);
        }
        public static async Task ReportError(HttpResponseMessage response, IUser user, IGuild guild, IChannel channel, IMessage? message)
        {
            var cont = Program.Core.GetRequiredService<ErrorReportController>();
            await cont.ReportHTTPError(response, user, guild, channel, message);
        }
        public static async Task ReportError(Exception response, ICommandContext context)
        {
            await ReportError(response,
                context.User,
                context.Guild,
                context.Channel,
                context.Message);
        }
        public static async Task ReportError(Exception response, IInteractionContext context)
        {
            await ReportError(response,
                context.User,
                context.Guild,
                context.Channel,
                null);
        }
        public static async Task ReportError(Exception response, IUser? user, IGuild? guild, IChannel? channel, IMessage? message)
        {
            var cont = Program.Core.GetRequiredService<ErrorReportController>();
            await cont.ReportError(response, user, guild, channel, message);
        }
        public static async Task ReportError(Exception exception, string extraText = "")
        {
            var cont = Program.Core.GetRequiredService<ErrorReportController>();
            await cont.ReportException(exception, extraText);
        }
        #endregion
    }
}
