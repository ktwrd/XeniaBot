using NLog;
using Sentry;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace XeniaBot.Shared;

public static class StartupGlue
{
    public static void CheckAll(Assembly assembly, string[] args)
    {
        var docker = args.FirstOrDefault() == "docker";
        if (docker)
        {
            Environment.SetEnvironmentVariable("_XENIA_RUNNING_IN_DOCKER", "true");
        }
        FeatureFlags.EnsureEnvironmentValue();

        InitializeNlog(assembly);
        CheckConfiguration(assembly);
    }

    public static void InitializeNlog(Assembly assembly)
    {
        const string prefix = "[StartupGlue] ";
        Console.WriteLine(prefix + "Initialize NLog");
        var relativeConfigurationLocation = FeatureFlags.NLogConfigLocation;
        if (File.Exists(relativeConfigurationLocation))
        {
            Console.WriteLine("Loading configuration from " + relativeConfigurationLocation);
            LogManager.Setup().LoadConfigurationFromFile(relativeConfigurationLocation);
        }
        else
        {
            var nlogInEntry = true;

            // get nlog in entry assembly
            var entryAsmName = assembly.GetName();
            if (entryAsmName != null)
            {
                var targetName = entryAsmName.FullName + ".nlog.config";
                var foundResourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(e => e.Equals(targetName, StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrEmpty(foundResourceName))
                {
                    nlogInEntry = false;
                }
                else
                {
                    Console.WriteLine(prefix + "Loaded NLog config from: " + entryAsmName.FullName);
                    LogManager.Setup().LoadConfigurationFromAssemblyResource(assembly, "nlog.config");
                }
            }

            if (!nlogInEntry)
            {
                Console.WriteLine(prefix + "Loaded NLog config from XeniaDiscord.Shared");
                LogManager.Setup()
                    .LoadConfigurationFromAssemblyResource(typeof(StartupGlue).Assembly, "nlog.config");
            }
        }

        if (!string.IsNullOrEmpty(FeatureFlags.SentryDsn.Trim()) && LogManager.Configuration != null)
        {
            LogManager.Configuration.AddSentry(
                opts =>
                {
                    opts.Dsn = FeatureFlags.SentryDsn;
                    opts.SendDefaultPii = true;
                    opts.MinimumBreadcrumbLevel = NLog.LogLevel.Trace;
                    opts.MinimumEventLevel = NLog.LogLevel.Warn;
                    opts.AttachStacktrace = true;
                    opts.DiagnosticLevel = SentryLevel.Debug;
                    opts.TracesSampleRate = 1.0;
#if DEBUG
                    opts.Debug = true;
#else
                    opts.Debug = false;
#endif
                });
        }
    }
    public static void CheckConfiguration(Assembly assembly)
    {
        var logger = LogManager.GetLogger(nameof(CheckConfiguration));
        try
        {
            // We know for sure that the default configuration location used
            // by Xenia is "/config", and that this will only happen on Docker.
            //
            // This is here just to be sure that the creation of the config
            // directory will not fail.
            if (FeatureFlags.RunningInDocker && !Directory.Exists("/config"))
            {
                Directory.CreateDirectory("/config");
            }

            logger.Info($"Using File: {FeatureFlags.XmlConfigLocation}");
            if (string.IsNullOrEmpty(FeatureFlags.XmlConfigLocation))
            {
                logger.Error($"Environment Variable CONFIG_LOCATION has not been set!!!");
                Environment.Exit(1);
                return;
            }

            // Create parent directory if it doesn't exist.
            var parentDirectory = Path.GetDirectoryName(FeatureFlags.XmlConfigLocation);
            if (!string.IsNullOrEmpty(parentDirectory))
            {
                if (!Directory.Exists(parentDirectory))
                {
                    Directory.CreateDirectory(parentDirectory);
                }
            }

            if (!File.Exists(FeatureFlags.XmlConfigLocation))
            {
                logger.Warn($"Configuration file does not exist!! Creating a blank one, PLEASE POPULATE IT !!! (output location: {FeatureFlags.XmlConfigLocation})");
                File.WriteAllText(FeatureFlags.XmlConfigLocation, string.Empty);
                if (TryGetExampleConfigurationStream(out var exampleConfigStream, assembly))
                {
                    using var file = new FileStream(
                        FeatureFlags.XmlConfigLocation,
                        FileMode.OpenOrCreate,
                        FileAccess.Write,
                        FileShare.Read);

                    file.SetLength(0);
                    file.Seek(0, SeekOrigin.Begin);
                    exampleConfigStream!.CopyTo(file);
                }
                else
                {
                    logger.Fatal($"Couldn't find embedded resource ending with \".config.example.xml\" in assembly {assembly}");
                }
                Environment.Exit(1);
                return;
            }
            else
            {
                logger.Info($"Configuration file found! ({FeatureFlags.XmlConfigLocation})");
            }
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Failed to check configuration!");
            Environment.Exit(1);
        }
    }
    private static bool TryGetExampleConfigurationStream(out Stream? stream, params Assembly[] assemblies)
    {
        foreach (var asm in assemblies)
        {
            var resourceName = asm.GetManifestResourceNames()
                .FirstOrDefault(e => e.Trim().EndsWith(".config.example.xml", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(resourceName?.Trim())) continue;

            stream = asm.GetManifestResourceStream(resourceName);
            if (stream != null) return true;
        }
        stream = null;
        return false;
    }
}
