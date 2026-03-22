using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Sentry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using XeniaBot.Core.Helpers;
using XeniaBot.Core.LevelSystem.Services;
using XeniaBot.Logic.Services;
using XeniaBot.MongoData.Repositories;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;
using XeniaDiscord;
using XeniaDiscord.Common;

namespace XeniaBot.Core;

public static class Program
{
    #region Properties
    private static readonly Logger log = LogManager.GetCurrentClassLogger();
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        IgnoreReadOnlyFields = false,
        IgnoreReadOnlyProperties = false,
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
            var result = VersionRaw ?? "unknown_version";
            if (ProgramDetails.Debug) result += "-DEBUG";
            return result;
        }
    }
    private static string? VersionRaw => UnderlyingVersion?.ToString() ?? null;
    internal static Version? UnderlyingVersion
    {
        get
        {
            var asm = Assembly.GetAssembly(typeof(Program));
            var name = asm?.GetName();
            if (name == null || name.Version == null)
            {
                if (name == null)
                {
                    log.Warn($"`Assembly.GetName()` resulted in null (assembly: {asm})");
                }
                else if (name.Version == null)
                {
                    log.Warn($"`Assembly.GetName().Version` is null (assembly: {asm})");
                }
                return null;
            }
            return name.Version;
        }
    }
    public static ProgramDetails ProgramDetails => new()
    {
        StartTimestamp = StartTimestamp,
        VersionRaw = UnderlyingVersion,
        Platform = XeniaPlatform.Bot,
        SetStatus = true,
        PlatformTag = "Master",
        Debug = Debug
    };
#if DEBUG
    private const bool Debug = true;
#else
    private const bool Debug = false;
#endif
    public static CoreContext Core { get; private set; }
    #endregion
    public static void Main(string[] args)
    {
        LogManager.Setup().LoadConfigurationFromFile(FeatureFlags.NLogFileLocation);

        StartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        Core = new CoreContext(ProgramDetails)
        {
            StartTimestamp = StartTimestamp,
            RegisterModules = CoreContextRegisterModules,
            RegisterDeveloperModules = CoreContextRegisterDeveloperModules
        };
        if (!string.IsNullOrEmpty(FeatureFlags.SentryDSN))
        {
            SentrySdk.Init(static options =>
            {
                Update(options);
            });
            LogManager.Configuration?.AddSentry(static options =>
            {
                Update(options);
            });
        }
        Core.MainAsync(args, CoreContextBeforeServiceBuild).Wait();
    }
    private static void Update(SentryOptions options)
    {
        options.Dsn = FeatureFlags.SentryDSN;
        options.Release = VersionRaw;
        options.SendDefaultPii = true;
        options.AttachStacktrace = true;
        options.Environment = ProgramDetails.Debug ? "production" : "debug";
        options.TracesSampleRate = 1.0;
        options.IsGlobalModeEnabled = false;
        options.Debug = ProgramDetails.Debug;
    }
    private static async Task CoreContextRegisterModules(InteractionService interactions, IServiceProvider services)
    {
        var transaction = SentryHelper.CreateTransaction();
        try
        {
            await XeniaDiscordCoreInteractions.RegisterModules(interactions, services);
            await XeniaDiscordInteractions.RegisterModules(interactions, services);
        }
        finally
        {
            transaction.Finish();
        }
    }
    private static async Task<ModuleInfo[]> CoreContextRegisterDeveloperModules(InteractionService interactions, IServiceProvider services)
    {
        var transaction = SentryHelper.CreateTransaction();
        var result = new List<ModuleInfo>();
        try
        {
            result.AddRange(await XeniaDiscordInteractions.RegisterDeveloperModules(interactions, services));
            result.AddRange(await XeniaDiscordInteractionsDataMigration.RegisterDeveloperModules(interactions, services));
        }
        finally
        {
            transaction.Finish();
        }
        return result.ToArray();
    }
    private static Task CoreContextBeforeServiceBuild(IServiceCollection services)
    {
        services.WithDatabaseServices();
        XeniaDiscordData.RegisterServices(services, true);
        XeniaDiscordCommon.RegisterServices(services, true);
        XeniaDiscordInteractionsDataMigration.RegisterServices(services);
        AttributeHelper.InjectControllerAttributes("XeniaBot.Shared", services);
        AttributeHelper.InjectControllerAttributes(typeof(XeniaVersionRepository).Assembly, services); // XeniaBot.Data
        AttributeHelper.InjectControllerAttributes("XeniaBot.Core", services);
        AttributeHelper.InjectControllerAttributes(typeof(ReminderService).Assembly, services); // XeniaBot.Logic
        AttributeHelper.InjectControllerAttributes(typeof(LevelSystemService).Assembly, services);
        return Task.CompletedTask;
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            SentrySdk.CaptureException(ex);
            log.Fatal(ex, $"Unhandled exception! ({nameof(e.IsTerminating)}: {e.IsTerminating})");
            if (Core.Services.GetRequiredService<DiscordService>().IsReady)
            {
                DiscordHelper.ReportError(ex, $"Unhandled exception! ({nameof(e.IsTerminating)}: {e.IsTerminating})").Wait();
            }
        }
        Console.Error.WriteLine("OH SHIT, UNHANDLED EXCEPTION!!!\n" + e.ExceptionObject?.ToString());
        if (Debug)
        {
            Debugger.Break();
        }
    }

    public static void Quit(int exitCode = 0)
    {
        Core.OnQuit(exitCode);
    }
}