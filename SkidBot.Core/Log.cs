using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core
{
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
            Background = ConsoleColor.Yellow,
            Foreground = ConsoleColor.Black
        };
        public static LogColor ErrorColor = new LogColor
        {
            Background = ConsoleColor.Red,
            Foreground = ConsoleColor.Black
        };
        public static LogColor DebugColor = new LogColor
        {
            Background = ConsoleColor.Blue,
            Foreground = ConsoleColor.White
        };
        public static LogColor DefaultColor = new LogColor
        {
            Background = ConsoleColor.Black,
            Foreground = ConsoleColor.White
        };
        #endregion

        private static List<string> linequeue = new List<string>();
        private static System.Timers.Timer _timer = null;
        public static string LogOutput => Path.Combine(Directory.GetCurrentDirectory(), "Logs", $"log_{Program.StartTimestamp}.txt");
        private static void CreateTimer()
        {
            if (_timer != null) return;
            if (!Directory.Exists(Path.GetDirectoryName(LogOutput)))
                Directory.CreateDirectory(Path.GetDirectoryName(LogOutput));
            _timer = new System.Timers.Timer();
            _timer.Interval = 5000;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Enabled = true;
            _timer.Start();
        }

        private static void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timer.Stop();
            string[] lines = linequeue.ToArray();
            linequeue.Clear();
            File.AppendAllLines(LogOutput, lines);
            _timer.Start();
        }

        public static void SetColor(LogColor? color = null)
        {
            LogColor targetColor = color ?? DefaultColor;
            Console.BackgroundColor = targetColor.Background;
            Console.ForegroundColor = targetColor.Foreground;
        }
        public static string WarnPrefix = "[WARN]";
        public static string ErrorPrefix = "[ERR] ";
        public static string LogPrefix = "[LOG] ";
        public static string DebugPrefix = "[DEBG]";
        public static bool ShowMethodName = true;
        public static bool ShowTimestamp = false;

        public static void Warn(string content, [CallerMemberName] string methodname = null, [CallerFilePath] string methodfile = null)
            => WriteLine(content, WarnColor, WarnPrefix, ShowMethodName, methodname, methodfile);
        public static void Error(string content, [CallerMemberName] string methodname = null, [CallerFilePath] string methodfile = null)
            => WriteLine(content, ErrorColor, ErrorPrefix, ShowMethodName, methodname, methodfile);
        public static void Debug(string content, [CallerMemberName] string methodname = null, [CallerFilePath] string methodfile = null)
            => WriteLine(content, DebugColor, DebugPrefix, ShowMethodName, methodname, methodfile);

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
            CreateTimer();
            SetColor(color);
            if (methodname != null && fetchMethodName && methodfile != null)
                content = $"{FormatMethodName(methodname, methodfile)}{content}";
            string pfx = (prefix ?? LogPrefix) + " ";
            Console.WriteLine(pfx + content);
            linequeue.Add(pfx + content);
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
}
