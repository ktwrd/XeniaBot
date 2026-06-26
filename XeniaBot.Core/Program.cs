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
        LogManager.Setup().LoadConfigurationFromFile(FeatureFlags.NLogFileLocation);
        if (!string.IsNullOrEmpty(FeatureFlags.SentryDSN))
        {
            SentrySdk.Init(Update);
            LogManager.Configuration ??= new();
            LogManager.Configuration!.AddSentry(Update);
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
            var cex = new XeniaFatalException(e, Core.Services.GetService<DiscordService>());
            log.Fatal(cex);
            SentryId? eventId = null;
            try
            {
                eventId = SentrySdk.CaptureException(cex);
            }
            catch (Exception iex)
            {
                log.Warn(iex, "Failed to report fatal exception");
            }

            try
            {
                var errorReportService = Core.Services.GetService<ErrorReportService>();
                if (Core.Services.GetService<DiscordService>()?.IsReady == true &&
                    errorReportService != null)
                {
                    errorReportService.Submit(new ErrorReportBuilder()
                        .WithException(ex)
                        .WithNotes("Unhandled exception" + (e.IsTerminating ? "\nApplication is terminating!" : string.Empty))).Wait();
                }
            }
            catch (Exception iex)
            {
                log.Fatal(iex, $"Failed to submit error for SentryId={eventId}");
            }

            try
            {
                SentrySdk.Flush(TimeSpan.FromSeconds(15));
            }
            catch (Exception iex)
            {
                log.Warn(iex, "Failed to flush sentry");
            }
        }
        else
        {
            log.Fatal("Unhandled exception!\n" + e.ExceptionObject);
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
        var r = (e.IsTerminating ? a + b : a);
        if (discordService == null) return r;
        return discordService.IsReady ? c : d;
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