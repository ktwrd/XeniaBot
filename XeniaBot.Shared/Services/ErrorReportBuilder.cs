using Discord;
using Discord.Commands;
using Sentry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Humanizer;

namespace XeniaBot.Shared.Services;

public class ErrorReportBuilder
{
    private string? _exceptionJson;
    private HttpResponseMessage? _httpResponseMessage;
    private string? _httpResponseText;

    private IGuild? _guild;
    private IChannel? _channel;
    private IUser? _user;
    private IMessage? _message;
    private IRole? _role;

    private ICommandContext? _commandContext;
    private IInteractionContext? _interactionContext;

    private readonly List<KeyValuePair<string, string>> _extraAttachments = [];

    public string? Notes { get; private set; }

    public Exception? Exception { get; private set; }

    public void BuildAttachments(ref List<FileAttachment> attachments)
    {
        Notes ??= Notes?.Trim();
        if (Notes?.Length >= 1024)
        {
            attachments.Add(new FileAttachment(
                new MemoryStream(Encoding.UTF8.GetBytes(Notes)),
                fileName: "notes.txt"));
        }
        if (Exception != null)
        {
            attachments.Add(new FileAttachment(
                new MemoryStream(Encoding.UTF8.GetBytes(Exception.ToString())),
                fileName: "exception.txt"));
        }

        _exceptionJson = _exceptionJson?.Trim();
        if (!string.IsNullOrWhiteSpace(_exceptionJson) &&
            !string.Equals("[]", _exceptionJson, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals("[null]", _exceptionJson, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals("{}", _exceptionJson, StringComparison.OrdinalIgnoreCase))
        {
            attachments.Add(new FileAttachment(
                new MemoryStream(Encoding.UTF8.GetBytes(_exceptionJson)),
                fileName: "exception.json"));
        }

        if (_extraAttachments?.Count > 0)
        {
            // only take first 9 attachments.
            // the rest are available in sentry.
            var attachmentCount = Math.Clamp(9 - _extraAttachments.Count - attachments.Count, 0, 9);
            foreach (var pair in _extraAttachments.Take(attachmentCount))
            {
                var fn = Path.GetFileNameWithoutExtension(pair.Key);
                var ex = Path.GetExtension(pair.Key);
                if (string.IsNullOrEmpty(fn)) ex = "attachment";
                if (string.IsNullOrEmpty(ex)) ex = ".txt";
                var tgtFn = fn + ex;
                attachments.Add(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(pair.Value)), fileName: tgtFn));
            }
        }
    }

    public EmbedBuilder BuildEmbed(ref List<FileAttachment> attachments)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Exception Caught")
            .WithColor(Color.Red)
            .WithCurrentTimestamp();

        Notes ??= Notes?.Trim();
        Notes ??= string.Empty;
        if (!string.IsNullOrWhiteSpace(Notes) &&
            Notes.Length is > 0 and < 1024)
        {
            embed.AddField("Notes", Notes);
        }

        if (_extraAttachments?.Count > 0)
        {
            var attachmentCount = Math.Clamp(9 - _extraAttachments.Count - attachments.Count, 0, 9);
            if (attachmentCount < _extraAttachments.Count)
            {
                var missedCount = _extraAttachments.Count - attachmentCount;
                embed.AddField(
                    "⚠️ Missing Attachments",
                    "Missing " + "attachment".ToQuantity(missedCount, "N0") +
                    " since only 9 can be uploaded in one message.");
            }
        }
        return embed;
    }

    internal void UpdateSentryScope(Scope scope)
    {
        var extra = new Dictionary<string, string>();
        if (_exceptionJson != null)
        {
            extra["exceptionJson"] = _exceptionJson;
        }
        if (_httpResponseMessage != null)
        {
            extra["httpResponseMessage.StatusCode"] = ((int)_httpResponseMessage.StatusCode).ToString();
            extra["httpResponseMessage.RequestUri"] = _httpResponseMessage.RequestMessage?.RequestUri?.ToString() ?? "";
            extra["httpResponseMessage.ContentHeaders"] = _httpResponseMessage.Content.Headers.ToString();
        }
        if (_httpResponseText != null)
        {
            extra["httpResponseMessage.Content"] = _httpResponseText;
        }
        for (int i = 0; i < _extraAttachments.Count; i++)
        {
            extra[$"extraAttachment[{i}] (name: {_extraAttachments[i].Key})"] = _extraAttachments[i].Value;
        }

        if (_guild != null)
        {
            extra["guild.id"] = _guild.Id.ToString();
            extra["guild.name"] = _guild.Name;
        }
        if (_user != null)
        {
            extra["user.id"] = _user.Id.ToString();
            extra["user.username"] = FormatUsername(_user);
        }
        if (_channel != null)
        {
            extra["message.id"] = _channel.Id.ToString();
            extra["message.name"] = _channel.Name;
        }
        if (_message != null)
        {
            extra["message.id"] = _message.Id.ToString();
            extra["message.channel.id"] = _message.Channel.Id.ToString();
            extra["message.channel.name"] = string.IsNullOrWhiteSpace(_message.Channel.Name) ? string.Empty : _message.Channel.Name;
        }
        if (_role != null)
        {
            extra["role.id"] = _role.Id.ToString();
            extra["role.name"] = _role.Name;
            extra["role.is_managed"] = _role.IsManaged.ToString();
            extra["role.guild.id"] = _role.Guild.Id.ToString();
            extra["role.guild.name"] = _role.Guild.Name;
        }
        if (_commandContext != null)
        {
            extra["commandContext"] = ErrorReportService.SerializeJsonSafe(_commandContext) ?? "";
            extra["commandContext.guild.id"] = _commandContext.Guild.Id.ToString();
            extra["commandContext.guild.name"] = _commandContext.Guild.Name;
            extra["commandContext.author.id"] = _commandContext.User.Id.ToString();
            extra["commandContext.author.username"] = FormatUsername(_commandContext.User);
            extra["commandContext.channel.id"] = _commandContext.Channel.Id.ToString();
            extra["commandContext.channel.name"] = _commandContext.Channel.Name;
            extra["commandContext.message.id"] = _commandContext.Message.Id.ToString();
        }
        if (_interactionContext != null)
        {
            extra["interactionContext.guild"] = ErrorReportService.SerializeJsonSafe(_interactionContext.Guild) ?? "";
            extra["interactionContext.channel"] = ErrorReportService.SerializeJsonSafe(_interactionContext.Channel) ?? "";
            extra["interactionContext.author"] = ErrorReportService.SerializeJsonSafe(_interactionContext.User, 1) ?? "";
            extra["interactionContext.interaction"] = ErrorReportService.SerializeJsonSafe(_interactionContext.Interaction) ?? "";
            extra["interactionContext.author.id"] = _interactionContext.User.Id.ToString();
            extra["interactionContext.author.username"] = FormatUsername(_interactionContext.User);
        }
        if (!string.IsNullOrEmpty(Notes?.Trim()))
        {
            extra["notes"] = Notes;
        }
        scope.SetExtras(extra.Select(static e => new KeyValuePair<string, object?>(e.Key, e.Value)));
    }

    private static string FormatUsername(IUser user)
    {
        return string.IsNullOrWhiteSpace(user.Discriminator?.Trim('0')) || user.DiscriminatorValue == 0
            ? user.Username
            : $"{user.Username}#{user.Discriminator}";
    }

    #region Builder Methods
    public ErrorReportBuilder WithException(Exception exception)
    {
        Exception = exception;
        _exceptionJson = ErrorReportService.SerializeJsonSafe(exception);
        return this;
    }
    public ErrorReportBuilder WithHttpResponseMessage(HttpResponseMessage httpResponseMessage)
    {
        _httpResponseMessage = httpResponseMessage;
        return this;
    }
    public ErrorReportBuilder WithHttpResponseText(string? text)
    {
        _httpResponseText = text;
        return this;
    }

    public ErrorReportBuilder WithGuild(IGuild? guild)
    {
        _guild = guild;
        return this;
    }
    public ErrorReportBuilder WithChannel(IChannel? channel)
    {
        _channel = channel;
        return this;
    }
    public ErrorReportBuilder WithUser(IUser? user)
    {
        _user = user;
        return this;
    }
    public ErrorReportBuilder WithMessage(IMessage? message)
    {
        _message = message;
        return this;
    }
    public ErrorReportBuilder WithRole(IRole? role)
    {
        _role = role;
        return this;
    }
    public ErrorReportBuilder WithContext(ICommandContext? commandContext)
    {
        _commandContext = commandContext;
        return this;
    }
    public ErrorReportBuilder WithContext(IInteractionContext? interactionContext)
    {
        _interactionContext = interactionContext;
        return this;
    }
    public ErrorReportBuilder WithNotes(string? notes)
    {
        Notes = notes;
        return this;
    }
    public ErrorReportBuilder AddAttachment(string filename, string content)
    {
        _extraAttachments.Add(new(filename, content));
        return this;
    }
    public ErrorReportBuilder AddSerializedAttachment<TData>(string filename, TData instance)
    {
        _extraAttachments.Add(new(filename, ErrorReportService.SerializeJsonSafe<TData>(instance) ?? ""));
        return this;
    }
    public ErrorReportBuilder AddAttachments(IEnumerable<KeyValuePair<string, string>> dict)
    {
        foreach (var kv in dict)
        {
            _extraAttachments.Add(kv);
        }
        return this;
    }
    #endregion
}
