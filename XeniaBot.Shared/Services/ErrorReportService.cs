using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Sentry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace XeniaBot.Shared.Services;

[XeniaController]
public class ErrorReportService : BaseService
{
    private static readonly Logger _log = LogManager.GetLogger("Xenia." + nameof(ErrorReportService));
    private readonly DiscordSocketClient _client;
    private readonly ConfigData _config;

    public ErrorReportService(IServiceProvider services)
        : base(services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        _config = services.GetRequiredService<ConfigData>();
        if (_config == null)
        {
            _log.Error($"_config is null!");
            Environment.Exit(1);
        }
        else if (_config.ErrorReporting == null)
        {
            _log.Error($"_config.ErrorReporting is null!");
            Environment.Exit(1);
        }
        else if (_config.ErrorReporting.GuildId == null)
        {
            _log.Error($"_config.ErrorReporting.GuildId is null!");
            Environment.Exit(1);
        }
        else if (_config.ErrorReporting.ChannelId == null)
        {
            _log.Error($"_config.ErrorReporting.ChannelId is null!");
            Environment.Exit(1);
        }
    }

    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        IgnoreReadOnlyFields = false,
        IgnoreReadOnlyProperties = false,
        IncludeFields = true,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.Preserve,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    #region HTTP Error Reporting
    [Obsolete("Use ErrorReportService.Submit(ErrorReportBuilder)")]
    public async Task ReportError(HttpResponseMessage response, ICommandContext commandContext)
    {
        await ReportHTTPError(response,
            commandContext.User,
            commandContext.Guild,
            commandContext.Channel,
            commandContext.Message);
    }
    [Obsolete("Use ErrorReportService.Submit(ErrorReportBuilder)")]
    public async Task ReportError(HttpResponseMessage response, IInteractionContext context)
    {
        await ReportHTTPError(response,
            context.User,
            context.Guild,
            context.Channel,
            null);
    }
    [Obsolete("Use ErrorReportService.Submit(ErrorReportBuilder)")]
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
    [Obsolete("Use ErrorReportService.Submit(ErrorReportBuilder)")]
    public async Task ReportError(Exception response, ICommandContext context)
    {
        await ReportError(response,
            context.User,
            context.Guild,
            context.Channel,
            context.Message);
    }

    [Obsolete("Use ErrorReportService.Submit(ErrorReportBuilder)")]
    public async Task ReportError(Exception response, IInteractionContext context)
    {
        await ReportError(response,
            context.User,
            context.Guild,
            context.Channel,
            null);
    }

    [Obsolete("Use ErrorReportService.Submit(ErrorReportBuilder)")]
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
        _log.Error($"Failed to process. User: {user?.Id}, Guild: {guild?.Id}, Channel: {channel?.Id}.\n{response}");
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

    /// <summary>
    /// Submit an error report.
    /// </summary>
    public async Task Submit(ErrorReportBuilder errorReportBuilder)
    {
        if (errorReportBuilder.Exception != null)
        {
            try
            {
                SentrySdk.CaptureException(errorReportBuilder.Exception, errorReportBuilder.UpdateSentryScope);
            }
            catch (Exception ex)
            {
                _log.Fatal(ex, $"Failed to report exception via SentrySdk");
            }
            var msg = "Reported Exception";
            if (!string.IsNullOrEmpty(errorReportBuilder.Notes))
            {
                msg += $": {errorReportBuilder.Notes}";
            }
            _log.Error(errorReportBuilder.Exception, msg);
        }

        List<FileAttachment> attachments;
        EmbedBuilder embed;
        try
        {
            attachments = [];
            errorReportBuilder.BuildAttachments(ref attachments);
            embed = errorReportBuilder.BuildEmbed(ref attachments);
        }
        catch (Exception ex)
        {
            _log.Fatal(ex, $"Failed to generate attachments & embed");
            return;
        }
        try
        {
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
        catch (Exception ex)
        {
            _log.Fatal(ex, $"Failed to report error to channel {_config.ErrorReporting.ChannelId} in guild {_config.ErrorReporting.GuildId}");
        }
    }

    [Obsolete("Use ErrorReportService.Submit(ErrorReportBuilder)")]
    public async Task ReportException(
        Exception exception,
        string notes = "",
        IReadOnlyDictionary<string, string>? extraAttachments = null)
    {
        /*
        var builder = new ErrorReportBuilder()
            .WithException(exception)
            .WithNotes(notes);
        if (extraAttachments != null)
        {
            builder.AddAttachments(extraAttachments);
        }
        await Submit(builder);
        */
        // var exceptionJson = SerializeJsonSafe(exception);
        SentrySdk.CaptureException(exception, (scope) =>
        {
            scope.SetExtra("notes", notes);
            // scope.SetExtra("exceptionJson", exceptionJson);
            if (extraAttachments?.Count > 0)
            {
                foreach (var key in extraAttachments.Keys)
                {
                    scope.SetExtra($"attachment[{key}]", extraAttachments[key]);
                }
            }
        });
        _log.Error($"Exception Reported\n{notes}\n{exception}");

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

        if (notes.Length >= 1024)
        {
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(notes));
            attachments.Add(new FileAttachment(ms, fileName: "notes.txt"));
        }
        else if (notes.Length > 0)
        {
            embed.AddField("Notes", notes);
        }
        
        if (extraAttachments?.Count > 0)
        {
            // only take first 9 attachments.
            // the rest are available in sentry.
            var attachmentCount = Math.Clamp(9 - extraAttachments.Count - attachments.Count, 0, 9);
            foreach (var pair in extraAttachments.Take(attachmentCount))
            {
                var fn = Path.GetFileNameWithoutExtension(pair.Key);
                var ex = Path.GetExtension(pair.Key);
                if (string.IsNullOrEmpty(fn)) ex = "attachment";
                if (string.IsNullOrEmpty(ex)) ex = ".txt";
                var tgtFn = fn + ex;
                attachments.Add(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(pair.Value)), fileName: tgtFn));
            }
            if (attachmentCount < extraAttachments.Count)
            {
                var missedCount = extraAttachments.Count - attachmentCount;
                var plural = missedCount == -1 || missedCount == 1 ? "" : "s";
                embed.AddField("⚠️ Missing Attachments", "Missing " + missedCount.ToString("n0") + $" attachment{plural} since only 9 can be uploaded in one message.");
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

    internal static string? SerializeJsonSafe<T>(T data, int? maxDepth = null)
    {
        string? result = null;
        try
        {
            var serializerOptions = SerializerOptions;
            if (maxDepth.HasValue && maxDepth > 0)
            {
                serializerOptions = new JsonSerializerOptions(SerializerOptions)
                {
                    MaxDepth = maxDepth.Value
                };
            }
            result = JsonSerializer.Serialize(data, serializerOptions);
        }
        catch (Exception ex)
        {
            _log.Warn(ex, $"Failed to serialize {typeof(T)}");
        }
        return result;
    }
}
