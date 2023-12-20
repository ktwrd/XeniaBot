using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace XeniaBot.Shared;

public class LoggerPolyfill : ILogger
{
    public void Log<TState>(LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        string data = formatter(state, exception);
        data = $"[{eventId}] {data}";
        switch (logLevel)
        {
            case LogLevel.Trace:
                XeniaBot.Shared.Log.Trace(data);
                break;
            case LogLevel.Debug:
                Shared.Log.Debug(data);
                break;
            case LogLevel.Information:
                Shared.Log.WriteLine(data);
                break;
            case LogLevel.Warning:
                Shared.Log.Warn(data);
                break;
            case LogLevel.Error:
                Shared.Log.Error(data);
                break;
            case LogLevel.Critical:
                Shared.Log.Critical(data);
                break;
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }
}

public class LoggerPolyfillT<T> : ILogger<T>
{
    public void Log<TState>(LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        string data = formatter(state, exception);
        data = $"[{eventId}] {data}";
        switch (logLevel)
        {
            // case LogLevel.Trace:
            //     XeniaBot.Shared.Log.Trace(data);
            //     break;
            // case LogLevel.Debug:
            //     Shared.Log.Debug(data);
            //     break;
            case LogLevel.Information:
                Shared.Log.WriteLine(data);
                break;
            case LogLevel.Warning:
                Shared.Log.Warn(data);
                break;
            case LogLevel.Error:
                Shared.Log.Error(data);
                break;
            case LogLevel.Critical:
                Shared.Log.Critical(data);
                break;
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }
}

public static class Log
{
    public struct LogColor
    {
        public ConsoleColor Foreground;
        public ConsoleColor Background;
    }
    #region Init Colors
    public static LogColor WarnColor = new LogColor
    {
        Background = ConsoleColor.DarkYellow,
        Foreground = ConsoleColor.White
    };
    public static LogColor ErrorColor = new LogColor
    {
        Background = ConsoleColor.DarkRed,
        Foreground = ConsoleColor.White
    };
    public static LogColor DebugColor = new LogColor
    {
        Background = ConsoleColor.DarkBlue,
        Foreground = ConsoleColor.White
    };

    public static LogColor NoteColor = new LogColor
    {
        Background = ConsoleColor.Magenta,
        Foreground = ConsoleColor.Black
    };
    public static LogColor DefaultColor = new LogColor
    {
        Background = ConsoleColor.Black,
        Foreground = ConsoleColor.White
    };
    #endregion

    public static void SetColor(LogColor? color = null)
    {
        if (!FeatureFlags.EnableLogColor)
            return;
        LogColor targetColor = color ?? DefaultColor;
        Console.BackgroundColor = targetColor.Background;
        Console.ForegroundColor = targetColor.Foreground;
    }
    public static string WarnPrefix = "[WARN]";
    public static string CriticalPrefix = "[CRIT]";
    public static string ErrorPrefix = "[ERR] ";
    public static string LogPrefix = "[LOG] ";
    public static string DebugPrefix = "[DEBG]";
    public static string NotePrefix = "[NOTE]";
    public static string TracePrefix = "[TRCE]";
    public static bool ShowMethodName = true;
    public static bool ShowTimestamp = false;

    public static void Warn(string content, [CallerMemberName] string methodname = null,
        [CallerFilePath] string methodfile = null)
        => WriteLine(content, WarnColor, WarnPrefix, ShowMethodName, methodname, methodfile);
    
    public static void Critical(string content, [CallerMemberName] string methodname = null,
        [CallerFilePath] string methodfile = null)
        => WriteLine(content, ErrorColor, CriticalPrefix, ShowMethodName, methodname, methodfile);

    public static void Error(string content, [CallerMemberName] string methodname = null,
        [CallerFilePath] string methodfile = null)
        => WriteLine(content, ErrorColor, ErrorPrefix, ShowMethodName, methodname, methodfile);
    public static void Error(string content, Exception exception, [CallerMemberName] string methodname = null,
        [CallerFilePath] string methodfile = null)
        => WriteLine($"{content}\n{exception}", ErrorColor, ErrorPrefix, ShowMethodName, methodname, methodfile);

    public static void Debug(string content, [CallerMemberName] string methodname = null,
        [CallerFilePath] string methodfile = null)
        => WriteLine(content, DebugColor, DebugPrefix, ShowMethodName, methodname, methodfile);

    public static void Note(string content, [CallerMemberName] string methodname = null,
        [CallerFilePath] string methodfile = null)
        => WriteLine(content, NoteColor, NotePrefix, ShowMethodName, methodfile, methodfile);
    
    public static void Trace(string content, [CallerMemberName] string methodname = null,
        [CallerFilePath] string methodfile = null)
        => WriteLine(content, ErrorColor, TracePrefix, ShowMethodName, methodfile, methodfile);

    #region Object Overload
    public static void Warn(object content, [CallerMemberName] string methodname = null, [CallerFilePath] string methodfile = null)
        => Warn(content.ToString(), methodname, methodfile);
    public static void Error(object content, [CallerMemberName] string methodname = null, [CallerFilePath] string methodfile = null)
        => Error(content.ToString(), methodname, methodfile);
    public static void Debug(object content, [CallerMemberName] string methodname = null, [CallerFilePath] string methodfile = null)
        => Debug(content.ToString(), methodname, methodfile);
    public static void WriteLine(object content, [CallerMemberName] string methodname = null, [CallerFilePath] string methodfile = null)
        => WriteLine(content.ToString(), methodname, methodfile);
    #endregion

    public static void WriteLine(string content, LogColor? color = null, string prefix = null, bool fetchMethodName = true, [CallerMemberName] string methodname = null, [CallerFilePath] string methodfile = null)
    {
        SetColor(color);
        if (methodname != null && fetchMethodName && methodfile != null)
            content = $"{FormatMethodName(methodname, methodfile)}{content}";
        string pfx = (prefix ?? LogPrefix) + " ";
        Console.WriteLine(pfx + content);
        SetColor();
    }
    private static string FormatMethodName(string methodName, string methodFilePath)
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (methodName != null)
        {
            if (methodFilePath != null)
                if (ShowTimestamp)
                    return $"[{Path.GetFileNameWithoutExtension(methodFilePath)}->{methodName}:{ts}] ";
                else
                    return $"[{Path.GetFileNameWithoutExtension(methodFilePath)}->{methodName}] ";
            if (ShowTimestamp)
                return $"[unknown->{methodName}:{ts}] ";
            return $"[unknown->{methodName}] ";
        }
        else if (methodFilePath != null)
            if (ShowTimestamp)
                return $"[{Path.GetFileNameWithoutExtension(methodFilePath)}:{ts}] ";
            else
                return $"[{Path.GetFileNameWithoutExtension(methodFilePath)}] ";
        if (ShowTimestamp)
            return $"[{ts}] ";
        return "";
    }
}