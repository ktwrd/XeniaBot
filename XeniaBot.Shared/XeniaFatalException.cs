using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using XeniaBot.Shared.Services;

namespace XeniaBot.Shared;

public class XeniaFatalException : Exception
{
    public XeniaFatalException(
        UnhandledExceptionEventArgs eventArgs,
        DiscordService? discordService)
        : base(FormatMessage(eventArgs, discordService), GetException(eventArgs))
    {
        IsTerminating = eventArgs.IsTerminating;
        IsDiscordReady = discordService?.IsReady;
    }

    private static string FormatMessage(UnhandledExceptionEventArgs e, DiscordService? discordService)
    {
        const string a = "Unhandled exception";
        const string b = " - Application is terminating!";
        const string c = " (discord was ready)";
        const string d = " (discord was not ready)";
        var s = new StringBuilder(a);
        if (e.IsTerminating) s.Append(b);
        if (discordService != null)
        {
            if (discordService.IsReady) s.Append(c);
            else s.Append(d);
        }
        return s.ToString();
    }

    private static Exception GetException(UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex) return ex;
        string? jsonData;
        try
        {
            jsonData = JsonSerializer.Serialize(e, SerializerOptions);
        }
        catch
        {
            jsonData = e.ExceptionObject?.ToString();
        }

        return new Exception($"Type: {e.ExceptionObject?.GetType()}\n{jsonData}");
    }

    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions()
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.Preserve,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
    
    public bool IsTerminating { get; }
    
    public bool? IsDiscordReady { get; }
}