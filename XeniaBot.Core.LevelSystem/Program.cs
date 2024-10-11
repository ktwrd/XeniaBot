using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using XeniaBot.Core.Helpers;
using XeniaBot.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using XeniaBot.Data;
using XeniaBot.Shared.Services;
using CronNET;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using XeniaBot.Data.Services;

namespace XeniaBot.Core.LevelSystem
{
    public static class Program
    {
        #region Fields
        public static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions()
        {
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            IncludeFields = true,
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.Preserve
        };
        /// <summary>
        /// UTC of <see cref="DateTimeOffset.ToUnixTimeSeconds()"/>
        /// </summary>
        public static long StartTimestamp { get; set; }

        public static string Version
        {
            get
            {
                string result = "";
                var targetAppend = VersionRaw;
                result += targetAppend ?? "null_version";
#if DEBUG
                result += "-DEBUG";
#endif
                return result;
            }
        }

        private static string? VersionRaw
        {
            get
            {
                return VersionReallyRaw?.ToString() ?? null;
            }
        }

        internal static Version? VersionReallyRaw
        {
            get
            {
                var asm = Assembly.GetAssembly(typeof(Program));
                var name = asm?.GetName();
                if (name == null || name.Version == null)
                {
                    if (name == null)
                    {
                        Log.Warn($"Assembly.GetName() resulted in null (when Assembly is from {asm?.Location})");
                    }
                    else if (name.Version == null)
                    {
                        Log.Warn($"Assembly.GetName().Version is null (when Assembly is from {asm?.Location})");
                    }
                    return null;
                }
                return name.Version;
            }
        }
        #endregion
        public static CoreContext Core { get; private set; }
        public static void Main(string[] args)
        {
            StartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            if (!string.IsNullOrEmpty(FeatureFlags.SentryDSN))
            {
                SentrySdk.Init(new SentryOptions()
                {
                    Dsn = FeatureFlags.SentryDSN,
                    TracesSampleRate = 1.0,
                    IsGlobalModeEnabled = true,
                    #if DEBUG
                    Debug = true
                    #else
                    Debug = false
                    #endif
                });
            }

            Core = new CoreContext(ProgramDetails);
            Core.StartTimestamp = StartTimestamp;
            Core.MainAsync(args, (s) =>
            {
                AttributeHelper.InjectControllerAttributes("XeniaBot.Shared", s);
                AttributeHelper.InjectControllerAttributes(typeof(BanSyncService).Assembly, s); // XeniaBot.Data
                AttributeHelper.InjectControllerAttributes("XeniaBot.Core", s);
                return Task.CompletedTask;
            }).Wait();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var except = (Exception)e.ExceptionObject;
            Console.Error.WriteLine(except);
            if (Core.Services.GetRequiredService<DiscordService>().IsReady)
            {
                DiscordHelper.ReportError(except).Wait();
            }
#if DEBUG
            Debugger.Break();
#endif
        }

        public static void Quit(int exitCode = 0)
        {
            Core.OnQuit(exitCode);
        }

        public static ProgramDetails ProgramDetails => new()
        {
            StartTimestamp = StartTimestamp,
            VersionRaw = VersionReallyRaw,
            Platform = XeniaPlatform.Bot,
            PlatformTag = "LevelSystem",
            Debug =
#if DEBUG
                true
#else
                false
#endif
        };

    }
}