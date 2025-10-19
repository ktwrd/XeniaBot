using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Sentry;

namespace XeniaBot.Shared.Services;

[XeniaController]
public class ErrorReportService : BaseService
{
    private readonly DiscordSocketClient _client;
    private readonly ConfigData _config;

    public ErrorReportService(IServiceProvider services)
        : base(services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        _config = services.GetRequiredService<ConfigData>();
        if (_config == null)
        {
            Log.Error($"_config is null!");
            Environment.Exit(1);
        }
        else if (_config.ErrorReporting == null)
        {
            Log.Error($"_config.ErrorReporting is null!");
            Environment.Exit(1);
        }
        else if (_config.ErrorReporting.GuildId == null)
        {
            Log.Error($"_config.ErrorReporting.GuildId is null!");
            Environment.Exit(1);
        }
        else if (_config.ErrorReporting.ChannelId == null)
        {
            Log.Error($"_config.ErrorReporting.ChannelId is null!");
            Environment.Exit(1);
        }
    }

    public static JsonSerializerOptions SerializerOptions => new JsonSerializerOptions()
    {
        IgnoreReadOnlyFields = true,
        IgnoreReadOnlyProperties = true,
        IncludeFields = true,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.Preserve
    };

    #region HTTP Error Reporting
    public async Task ReportError(HttpResponseMessage response, ICommandContext commandContext)
    {
        await ReportHTTPError(response,
            commandContext.User,
            commandContext.Guild,
            commandContext.Channel,
            commandContext.Message);
    }
    public async Task ReportError(HttpResponseMessage response, IInteractionContext context)
    {
        await ReportHTTPError(response,
            context.User,
            context.Guild,
            context.Channel,
            null);
    }
    public async Task ReportHTTPError(HttpResponseMessage response,
        IUser user,
        IGuild guild,
        IChannel channel,
        IMessage? message)
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
        var attachments = new List<FileAttachment>();

        GenerateAttachments(
            embed,
            attachments,
            null,
            stack,
            null);

        embed.AddField("HTTP Details", string.Join("\n", new string[]
        {
            "```",
            $"Code: {response.StatusCode} ({(int)response.StatusCode})",
            $"URL: {response.RequestMessage?.RequestUri}",
            "```"
        }));
        var responseHeadersText = string.Join("\n", new string[]
        {
            "```",
            JsonSerializer.Serialize(response.Headers, SerializerOptions),
            JsonSerializer.Serialize(response.TrailingHeaders, SerializerOptions),
            JsonSerializer.Serialize(response.Content.Headers, SerializerOptions),
            "```"
        });
        embed.AddField("Response Headers", responseHeadersText);

        var logGuild = _client.GetGuild(_config.ErrorReporting.GuildId);
        var logChannel = logGuild.GetTextChannel(_config.ErrorReporting.ChannelId);
        await logChannel.SendMessageAsync(embed: embed.Build());
    }
    #endregion

    #region Report Discord Context Error
    public async Task ReportError(Exception response, ICommandContext context)
    {
        await ReportError(response,
            context.User,
            context.Guild,
            context.Channel,
            context.Message);
    }
    public async Task ReportError(Exception response, IInteractionContext context)
    {
        await ReportError(response,
            context.User,
            context.Guild,
            context.Channel,
            null);
    }
    public async Task ReportError(Exception response,
        IUser? user,
        IGuild? guild,
        IChannel? channel,
        IMessage? message)
    {
        SentrySdk.CaptureException(response, (scope) =>
        {
            scope.SetExtra("user", user);
            scope.SetExtra("guild", guild);
            scope.SetExtra("channel", channel);
            scope.SetExtra("message", message);
        });
        Log.Error($"Failed to process. User: {user?.Id}, Guild: {guild?.Id}, Channel: {channel?.Id}.\n{response}");
        var stack = Environment.StackTrace;
        var embed = new EmbedBuilder()
        {
            Title = "Uncaught Exception",
            Description = "Uncaught Exception. Full exception is attached\n" + string.Join("\n", new string[]
            {
                "```",
                $"Author: {user?.Username}#{user?.Discriminator} ({user?.Id})",
                $"Guild: {guild?.Name} ({guild?.Id})",
                $"Channel: {channel?.Name} ({channel?.Id})",
                "```"
            }),
            Color = Color.Red
        };

        var attachments = new List<FileAttachment>();

        GenerateAttachments(
            embed,
            attachments,
            null,
            stack,
            response.ToString());

        var errGuild = _client.GetGuild(_config.ErrorReporting.GuildId);
        var errChannel = errGuild.GetTextChannel(_config.ErrorReporting.ChannelId);

        await errChannel.SendFilesAsync(attachments: attachments, text: "", embed: embed.Build());
    }
    #endregion

    public async Task ReportException(Exception exception, string notes = "",
        IReadOnlyDictionary<string, string>? extraAttachments = null)
    {
        if (extraAttachments?.Count > 9)
            throw new ArgumentOutOfRangeException(nameof(extraAttachments), $"Too many attachments! (limit: 9, got: {extraAttachments.Count})");
        SentrySdk.CaptureException(exception, (scope) =>
        {
            scope.SetExtra("notes", notes);
        });
        Log.Error($"Exception Reported\n{notes}\n{exception}");

        var stack = Environment.StackTrace;
        var exceptionContent = exception.ToString();

        var embed = new EmbedBuilder()
        {
            Title = "Exception Caught",
            Color = Color.Red
        }.WithCurrentTimestamp();
        var attachments = new List<FileAttachment>();

        GenerateAttachments(
            embed,
            attachments,
            null,
            stack,
            exceptionContent);

        if (notes.Length > 1000)
        {
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(notes));
            attachments.Add(new FileAttachment(ms, fileName: "notes.txt"));
        }
        else if (notes.Length > 0)
        {
            embed.AddField("Notes", $"```\n{notes}\n```");
        }
        
        if (extraAttachments?.Count > 0)
        {
            foreach (var pair in extraAttachments)
            {
                var fn = Path.GetFileNameWithoutExtension(pair.Key);
                var ex = Path.GetExtension(pair.Key);
                if (string.IsNullOrEmpty(fn)) ex = "attachment";
                if (string.IsNullOrEmpty(ex)) ex = ".txt";
                var tgtFn = fn + ex;
                /*int? c = null;
                while (attachments.Any(e => e.FileName == tgtFn))
                {
                    if (c == null) c = 0;
                    c++;
                    
                    tgtFn = $"{fn} ({c}){ex}";
                }*/
                attachments.Add(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(pair.Value)), fileName: tgtFn));
            }
        }

        var guild = _client.GetGuild(_config.ErrorReporting.GuildId);
        var textChannel = guild.GetTextChannel(_config.ErrorReporting.ChannelId);

        if (attachments.Count > 0)
        {
            await textChannel.SendFilesAsync(attachments, text: "", embed: embed.Build());
        }
        else
        {
            await textChannel.SendMessageAsync(embed: embed.Build());
        }
    }

    private void GenerateAttachments(
        EmbedBuilder embed,
        List<FileAttachment> attachments,
        string? message,
        string? stack,
        string? exception)
    {
        if (!string.IsNullOrEmpty(message))
        {
            if (message.Length > 1024)
            {
                embed.AddField("Message Content", "Attached as `messageContent.txt`");
                embed.WithDescription($"Exception is attached as `exception.txt`");
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(message));
                attachments.Add(new FileAttachment(ms, fileName: "messageContent.txt"));
            }
            else
            {
                embed.AddField("Message Content", message);
            }
        }

        if (!string.IsNullOrEmpty(stack))
        {
            if (stack.Length > 1024)
            {
                embed.AddField("Stack Trace", "Attached as `stack.txt`");
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(stack));
                attachments.Add(new FileAttachment(ms, fileName: "stack.txt"));
            }
            else
            {
                embed.AddField("Stack Trace", stack);
            }
        }

        if (!string.IsNullOrEmpty(exception))
        {
            if (exception.Length > 1024)
            {
                embed.AddField("Exception", "Attached as `exception.txt`");
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(exception));
                attachments.Add(new FileAttachment(ms, fileName: "exception.txt"));
            }
            else
            {
                embed.AddField("Exception", exception);
            }
        }
    }
}