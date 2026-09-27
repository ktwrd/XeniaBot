using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Sentry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;
using Prometheus;
using Sentry.AspNetCore;
using XeniaBot.Core.LevelSystem.Services;
using XeniaBot.Logic.Services;
using XeniaBot.MongoData.Repositories;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;
using XeniaDiscord;
using XeniaDiscord.Data;
using XeniaDiscord.Hosting;

namespace XeniaBot.Core;

public static class Program
{
    #region Properties
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
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
                    Log.Warn($"`Assembly.GetName()` resulted in null (assembly: {asm})");
                }
                else if (name.Version == null)
                {
                    Log.Warn($"`Assembly.GetName().Version` is null (assembly: {asm})");
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
    #endregion
    public static void Main(string[] args)
    {
        StartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        LogManager.Setup().LoadConfigurationFromFile(FeatureFlags.NLogFileLocation);
        if (!string.IsNullOrEmpty(FeatureFlags.SentryDSN))
        {
            SentrySdk.Init(Update);
            LogManager.Configuration?.AddSentry(Update);
        }
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Logging.ClearProviders();
        builder.Host.UseNLog();

        builder.Host.UseXeniaCore(new HostExtensions.XeniaCoreOptions()
        {
            UseCommands = true,
            Database = new HostExtensions.DatabaseServicesOptions()
            {
                DatabaseDeveloperPageExceptionFilter = false,
                EnableSensitiveDataLogging = true
            }
        });
        builder.WebHost.UseSentry(ConfigureSentry);
        
        builder.Services.AddSingleton(ProgramDetails);
        builder.Services.AddSingleton(
            new InteractionHandlerCallbacks()
            {
                RegisterModules = CoreContextRegisterModules,
                RegisterDeveloperModules = CoreContextRegisterDeveloperModules
            });
        XeniaDiscordCommon.RegisterServices(builder.Services);
        XeniaDiscordInteractionsDataMigration.RegisterServices(builder.Services);
        AttributeHelper.InjectControllerAttributes("XeniaBot.Shared", builder.Services);
        AttributeHelper.InjectControllerAttributes(typeof(XeniaVersionRepository).Assembly, builder.Services); // XeniaBot.Data
        AttributeHelper.InjectControllerAttributes("XeniaBot.Core", builder.Services);
        AttributeHelper.InjectControllerAttributes(typeof(ReminderService).Assembly, builder.Services); // XeniaBot.Logic
        AttributeHelper.InjectControllerAttributes(typeof(LevelSystemService).Assembly, builder.Services);

        builder.Services.AddHttpLogging();
        builder.Services.AddHttpClient();
        builder.Services.UseHttpClientMetrics();
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<XeniaDbContext>()
            .ForwardToPrometheus();

        var app = builder.Build();
        Application = app;
        AppDomain.CurrentDomain.UnhandledException += (a, b) => CurrentDomain_UnhandledException(app.Services, a, b);

        app.UseHttpLogging();
        app.UseHttpMetrics();
        
        app.MapGet("/status", HealthServer.MapHealthGet);
        app.MapMetrics();
        app.MapHealthChecks("/healthz");
        
        try
        {
            app.Run();
        }
        finally
        {
            LogManager.Shutdown();
            SentrySdk.Flush();
        }
    }
    private static WebApplication? Application { get; set; }
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

    private static void ConfigureSentry(WebHostBuilderContext context, SentryAspNetCoreOptions options)
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

        return [.. result];
    }

    private static void CurrentDomain_UnhandledException(
        IServiceProvider services,
        object sender,
        UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            var cex = new XeniaFatalException(e, services.GetService<DiscordService>());
            Log.Fatal(cex);
            SentryId? eventId = null;
            try
            {
                eventId = SentrySdk.CaptureException(cex);
            }
            catch (Exception iex)
            {
                Log.Warn(iex, "Failed to report fatal exception");
            }

            try
            {
                var errorReportService = services.GetService<ErrorReportService>();
                if (services.GetService<DiscordService>()?.IsReady == true &&
                    errorReportService != null)
                {
                    errorReportService.Submit(new ErrorReportBuilder()
                        .WithException(ex)
                        .WithNotes("Unhandled exception" + (e.IsTerminating ? "\nApplication is terminating!" : string.Empty))).Wait();
                }
            }
            catch (Exception iex)
            {
                Log.Fatal(iex, $"Failed to submit error for SentryId={eventId}");
            }

            try
            {
                SentrySdk.Flush(TimeSpan.FromSeconds(15));
            }
            catch (Exception iex)
            {
                Log.Warn(iex, "Failed to flush sentry");
            }
        }
        else
        {
            Log.Fatal("Unhandled exception!\n" + e.ExceptionObject);
        }
        Console.Error.WriteLine("OH SHIT, UNHANDLED EXCEPTION!!!\n" + e.ExceptionObject?.ToString());
        if (Debug)
        {
            Debugger.Break();
        }
    }

    public static void Quit(int exitCode = 0)
    {
        if (Application == null) Environment.Exit(exitCode);
        Application?.StopAsync(TimeSpan.FromMinutes(5));
        Environment.Exit(exitCode);
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