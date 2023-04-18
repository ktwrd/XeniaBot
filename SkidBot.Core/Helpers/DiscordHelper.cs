using Discord.Commands;
using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static SkidBot.Core.ConfigManager;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace SkidBot.Core.Helpers
{
    public static class DiscordHelper
    {
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
        public static async Task ReportError(HttpResponseMessage response, IUser user, IGuild guild, IMessageChannel channel, IUserMessage? message)
        {
            var client = Program.Services.GetRequiredService<DiscordSocketClient>();
            var stack = Environment.StackTrace;
            var embed = new EmbedBuilder()
            {
                Title = "Failed to execute message!",
                Description = "Failed to send HTTP Request.\n" + string.Join("\n", new string[]
                {
                    "```",
                    $"Author: {user.Username}#{user.Discriminator} ({user.Id})",
                    $"Guild: {guild.Name} ({guild.Id})",
                    $"Channel: {channel.Name} ({channel.Id})",
                    "```"
                })
            };

            embed.AddField("Stack Trace", $"```\n{stack}\n```");
            if (message != null)
                embed.AddField("Message Content", $"```\n{message.Content}\n```");
            embed.AddField("HTTP Details", string.Join("\n", new string[]
            {
                "```",
                $"Code: {response.StatusCode} ({(int)response.StatusCode})",
                $"URL: {response.RequestMessage?.RequestUri}",
                "```"
            }));
            embed.AddField("Resposne Headers", string.Join("\n", new string[]
            {
                "```",
                JsonSerializer.Serialize(response.Headers, Program.SerializerOptions),
                JsonSerializer.Serialize(response.TrailingHeaders, Program.SerializerOptions),
                JsonSerializer.Serialize(response.Content.Headers, Program.SerializerOptions),
                "```"
            }));

            await client
                .GetGuild(Program.Config.ErrorGuild)
                .GetTextChannel(Program.Config.ErrorChannel)
                .SendMessageAsync(embed: embed.Build());
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
        public static async Task ReportError(Exception response, IUser user, IGuild guild, IMessageChannel channel, IUserMessage? message)
        {
            var client = Program.Services.GetRequiredService<DiscordSocketClient>();
            var stack = Environment.StackTrace;
            var embed = new EmbedBuilder()
            {
                Title = "Uncaught Exception",
                Description = "Uncaught Exception. Full exception is attached\n" + string.Join("\n", new string[]
                {
                    "```",
                    $"Author: {user.Username}#{user.Discriminator} ({user.Id})",
                    $"Guild: {guild.Name} ({guild.Id})",
                    $"Channel: {channel.Name} ({channel.Id})",
                    "```"
                }),
                Color = Color.Red
            };

            embed.AddField("Stack Trace", $"```\n{stack}\n```");
            if (message != null)
                embed.AddField("Message Content", $"```\n{message.Content}\n```");

            await client
                .GetGuild(Program.Config.ErrorGuild)
                .GetTextChannel(Program.Config.ErrorChannel)
                .SendFileAsync(
                    stream: new MemoryStream(Encoding.UTF8.GetBytes(response.ToString())),
                    filename: "exception.txt",
                    text: "",
                    embeds: new Embed[] { embed.Build() });
        }
        public static async Task ReportError(Exception exception)
        {
            var stack = Environment.StackTrace;
            var exceptionContent = exception.ToString();
            var embed = new EmbedBuilder()
            {
                Title = "Uncaught Exception",
                Description = "```\n" + exceptionContent.Substring(0, Math.Min(exceptionContent.Length, 4080)) + "\n```"
            };

            var client = Program.Services.GetRequiredService<DiscordSocketClient>();

            var textChannel = client.GetGuild(Program.Config.ErrorChannel).GetTextChannel(Program.Config.ErrorChannel);
            var attachments = new FileAttachment[]
            {
                new FileAttachment(stream: new MemoryStream(Encoding.UTF8.GetBytes(stack)), fileName: "stack.txt"),
                new FileAttachment(stream: new MemoryStream(Encoding.UTF8.GetBytes(exceptionContent)), fileName: "exception.txt")
            };
            await textChannel.SendFilesAsync(attachments, text: "", embed: embed.Build());
        }
        #endregion
    }
}
