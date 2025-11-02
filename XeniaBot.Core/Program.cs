using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Helpers;
using XeniaBot.Shared;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using XeniaBot.Shared.Services;
using XeniaBot.Core.LevelSystem.Services;
using XeniaBot.Data.Services;
using XeniaBot.Logic.Services;
using Sentry;
using NLog;

namespace XeniaBot.Core
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
            ReferenceHandler = ReferenceHandler.Preserve,
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
                        LogManager.GetLogger("Main").Warn($"Assembly.GetName() resulted in null (when Assembly is from {asm?.Location})");
                    }
                    else if (name.Version == null)
                    {
                        LogManager.GetLogger("Main").Warn($"Assembly.GetName().Version is null (when Assembly is from {asm?.Location})");
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
            LogManager.Setup().LoadConfigurationFromFile(FeatureFlags.NLogFileLocation);

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
                LogManager.Configuration.AddSentry(options =>
                {
                    options.Dsn = FeatureFlags.SentryDSN;
                    options.TracesSampleRate = 1.0;
                    options.IsGlobalModeEnabled = true;
#if DEBUG
                    options.Debug = true;
#else
                    options.Debug = false;
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
                AttributeHelper.InjectControllerAttributes(typeof(ReminderService).Assembly, s); // XeniaBot.Logic
                AttributeHelper.InjectControllerAttributes(typeof(LevelSystemService).Assembly, s);
                return Task.CompletedTask;
            }).Wait();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                SentrySdk.CaptureException(ex);
            }
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
            SetStatus = true,
            PlatformTag = "Master",
            Debug =
#if DEBUG
                true
#else
                false
#endif
        };

    }
}