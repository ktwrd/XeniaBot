using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace XeniaBot.Shared.Controllers;

[BotController]
public class ErrorReportController : BaseController
{
    private readonly DiscordSocketClient _client;
    private readonly ConfigData _config;

    public ErrorReportController(IServiceProvider services)
        : base(services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        _config = services.GetRequiredService<ConfigData>();
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
        embed.AddField("Response Headers", string.Join("\n", new string[]
        {
            "```",
            JsonSerializer.Serialize(response.Headers, SerializerOptions),
            JsonSerializer.Serialize(response.TrailingHeaders, SerializerOptions),
            JsonSerializer.Serialize(response.Content.Headers, SerializerOptions),
            "```"
        }));

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
            
        bool attachStack = stack.Length > 1000;
        if (!attachStack)
            embed.AddField("Stack Trace", $"```\n{stack}\n```");
            
        if (message != null)
            embed.AddField("Message Content", $"```\n{message.Content}\n```");

        var errGuild = _client.GetGuild(_config.ErrorReporting.GuildId);
        var errChannel = errGuild.GetTextChannel(_config.ErrorReporting.ChannelId);
            
        var attachments = new List<FileAttachment>();
        var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(response.ToString()));
        attachments.Add(new FileAttachment(responseStream, "exception.txt"));

        var stackStream = new MemoryStream(Encoding.UTF8.GetBytes(stack));
        if (attachStack)
        {
            attachments.Add(new FileAttachment(stackStream, "stack.txt"));
        }

        await errChannel.SendFilesAsync(attachments: attachments, text: "", embed: embed.Build());
    }
    #endregion
    
    public async Task ReportException(Exception exception, string notes = "")
    {
        Log.Error($"Exception Reported\n{notes}\n{exception}");

        var stack = Environment.StackTrace;
        var exceptionContent = exception.ToString();

        var embed = new EmbedBuilder()
        {
            Title = "Exception Caught",
            Color = Color.Red
        }.WithCurrentTimestamp();
        var attachments = new List<FileAttachment>();

        var stackMs = new MemoryStream(Encoding.UTF8.GetBytes(stack));
        attachments.Add(new FileAttachment(stackMs, fileName: "stack.txt"));
        
        if (exceptionContent.Length > 1000)
        {
            embed.WithDescription($"Exception is attached as `exception.txt`");
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(exceptionContent));
            attachments.Add(new FileAttachment(ms, fileName: "exception.txt"));
        }
        else
        {
            embed.AddField("Exception", $"```\n{exception}\n```");
        }

        if (notes.Length > 1000)
        {
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(notes));
            attachments.Add(new FileAttachment(ms, fileName: "notes.txt"));
        }
        else if (notes.Length > 0)
        {
            embed.AddField("Notes", $"```\n{notes}\n```");
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
}