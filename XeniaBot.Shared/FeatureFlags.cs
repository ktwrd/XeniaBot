using System;
using System.IO;
using NLog;
using static XeniaBot.Shared.Helpers.EnvironmentHelper;

namespace XeniaBot.Shared;

public static class FeatureFlags
{
    private static string CurrentDirectory
    {
        get
        {
            var v = Directory.GetCurrentDirectory();
            if (string.IsNullOrEmpty(v))
            {
                v = "./";
            }

            return v;
        }
    }

    /// <summary>
    /// Key: LOG_TS
    /// Default: true
    ///
    /// Show timestamp in log entry.
    /// </summary>
    public static bool EnableLogTimestamp => ParseBool("LOG_TS", true);

    /// <summary>
    /// Key: DATA_DIR
    /// Default: ./data/
    ///
    /// Directory where all file-based data is stored. Used for caching and whatnot.
    /// </summary>
    public static string DataDirectory =>
        ParseString("DATA_DIR", Path.Join(CurrentDirectory, "data"));
    /// <summary>
    /// Key: DATA_DIR_FONTCACHE
    /// Default: ./fontcache/ (relative to <see cref="DataDirectory"/>)
    ///
    /// Directory where the font cache will be extracted to
    /// </summary>
    public static string FontCache =>
        ParseString("DATA_DIR_FONTCACHE", Path.Join(DataDirectory, "fontcache"));

    /// <summary>
    /// Legacy config file. Please do not use :3
    /// <para><b>Key:</b> <c>CONFIG_LOCATION</c></para>
    /// <para><b>Default Value:</b> <c>./config.json</c> (relative to <see cref="DataDirectory"/>)</para>
    /// </summary>
    public static string LegacyConfigLocation =>
        ParseString("CONFIG_LOCATION", Path.Join(DataDirectory, "config.json"));

    /// <summary>
    /// <para><b>Key:</b> <c>ConfigLocation</c></para>
    /// </summary>
    /// <remarks>
    /// Default value will be <c>/config/xenia.xml</c> when <see cref="RunningInDocker"/> is <see langword="true"/>, otherwise it'll be <c>./config.xml</c>.
    /// </remarks>
    public static string XmlConfigLocation => ParseString("ConfigLocation", RunningInDocker ? "/config/xenia.xml" : Path.Join(CurrentDirectory, "./config.xml"));

    /// <summary>
    /// <para><b>Key:</b> <c>SENTRY_DSN</c></para>
    ///
    /// Sentry DSN. Will be disabled when empty or null.
    /// </summary>
    public static string SentryDsn => ParseString("SENTRY_DSN", "");
    /// <summary>
    /// <para><b>Key:</b> <c>NLogConfigLocation</c></para>
    /// <para><b>Default Value:</b> <c>./nlog.config</c> (relative to <see cref="CurrentDirectory"/>)</para>
    /// </summary>
    public static string NLogConfigLocation => ParseString("NLogConfigLocation", DefaultNLogConfigLocation);

    /// <summary>
    /// <para><b>Key:</b> <c>_XENIA_RUNNING_IN_DOCKER</c></para>
    /// Please never set this value in your environment file, unless you know what you're doing.
    /// </summary>
    public static bool RunningInDocker => ParseBool("_XENIA_RUNNING_IN_DOCKER", false);
    public static bool ShowPrivateInformationWithAspNet => ParseBool("AspNet_ShowPrivateInformation", false);

    private static string DefaultNLogConfigLocation => Path.Combine(CurrentDirectory, "nlog.config");
    private const string AspNetEnvironmentName = "ASPNET_ENVIRONMENT";
    private const string DotNetEnvironmentName = "DOTNET_ENVIRONMENT";

    /// <summary>
    /// <para><b>Key:</b> <c>ASPNET_ENVIRONMENT</c></para>
    /// </summary>
    public static string AspNetEnvironment => ParseString(AspNetEnvironmentName, "");
    /// <summary>
    /// <para><b>Key:</b> <c>DOTNET_ENVIRONMENT</c></para>
    /// </summary>
    public static string DotNetEnvironment => ParseString(DotNetEnvironmentName, "");
    public static void SetEnvironment(string value)
    {
        Environment.SetEnvironmentVariable(AspNetEnvironmentName, value);
        Environment.SetEnvironmentVariable(DotNetEnvironmentName, value);
    }

    /// <summary>
    /// <para>Make sure that <see cref="AspNetEnvironment"/> and <see cref="DotNetEnvironment"/>
    /// equal to whatever one is set, when the other isn't set.</para>
    ///
    /// When <see cref="AspNetEnvironment"/> is set, and <see cref="DotNetEnvironment"/> isn't set, then this will set the value for <see cref="DotNetEnvironment"/> to be equal to <see cref="AspNetEnvironment"/>.
    /// Same thing is done, but with the environment variables swapped.
    /// </summary>
    public static void EnsureEnvironmentValue()
    {
        var log = LogManager.GetLogger(nameof(FeatureFlags) + "." + nameof(EnsureEnvironmentValue));
        if (string.IsNullOrEmpty(DotNetEnvironment) &&
            !string.IsNullOrEmpty(AspNetEnvironment))
        {
            log.Trace($"Updated {DotNetEnvironmentName} to match {AspNetEnvironmentName} ({AspNetEnvironment})");
            SetEnvironment(AspNetEnvironment);
        }
        else if (!string.IsNullOrEmpty(DotNetEnvironment) &&
                 string.IsNullOrEmpty(AspNetEnvironment))
        {
            log.Trace($"Updated {AspNetEnvironmentName} to match {DotNetEnvironmentName} ({DotNetEnvironment})");
            SetEnvironment(DotNetEnvironment);
        }
        else if (string.IsNullOrEmpty(DotNetEnvironment) && string.IsNullOrEmpty(AspNetEnvironment))
        {
            log.Trace($"{AspNetEnvironmentName} and {DotNetEnvironmentName} aren't set.");
        }
        else if (!string.IsNullOrEmpty(DotNetEnvironment) && !string.IsNullOrEmpty(AspNetEnvironment) &&
                 !DotNetEnvironment.Equals(AspNetEnvironment, StringComparison.InvariantCultureIgnoreCase))
        {
            log.Trace(string.Join(Environment.NewLine,
                $"{AspNetEnvironmentName} and {DotNetEnvironmentName} are set to different values!!!",
                $"{AspNetEnvironmentName}: {AspNetEnvironment}",
                $"{DotNetEnvironmentName}: {DotNetEnvironment}"));
        }
    }
}