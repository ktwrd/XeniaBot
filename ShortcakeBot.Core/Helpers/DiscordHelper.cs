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
using static ShortcakeBot.Core.ConfigManager;
using System.Text.Json;

namespace ShortcakeBot.Core.Helpers
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
        public static async Task ReportError(HttpResponseMessage response, IUser user, IGuild guild, IMessageChannel channel, IUserMessage message)
        {
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

            await Program.DiscordSocketClient
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
        public static async Task ReportError(Exception response, IUser user, IGuild guild, IMessageChannel channel, IUserMessage message)
        {
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

            await Program.DiscordSocketClient
                .GetGuild(Program.Config.ErrorGuild)
                .GetTextChannel(Program.Config.ErrorChannel)
                .SendFileAsync(
                    stream: new MemoryStream(Encoding.UTF8.GetBytes(response.ToString())),
                    filename: "exception.txt",
                    text: "",
                    embeds: new Embed[] { embed.Build() });
        }
    }
}
