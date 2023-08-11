using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NetVips;

namespace XeniaBot.Shared;

public static class FeatureFlags
{
    /// <summary>
    /// Parses an environment variable as a boolean. When trimmed&lowercased to `true` it will return true, but anything else will return `false`.
    /// When the environment variable isn't found, it wil default to <see cref="defaultValue"/>
    /// </summary>
    /// <param name="environmentKey"></param>
    /// <param name="defaultValue">Used when environment variable is not set.</param>
    /// <returns>`true` when envar is true, `false` when not true, <see cref="defaultValue"/> when not found.</returns>
    private static bool ParseBool(string environmentKey, bool defaultValue)
    {
        var item = Environment.GetEnvironmentVariable(environmentKey)
            ?? $"{defaultValue}";
        item = item.ToLower().Trim();
        return item == "true";
    }

    /// <summary>
    /// Just <see cref="Environment.GetEnvironmentVariable(string variable)"/> but when null it's <see cref="defaultValue"/>
    /// </summary>
    private static string ParseString(string environmentKey, string defaultValue)
    {
        return Environment.GetEnvironmentVariable(environmentKey) ?? defaultValue;
    }

    /// <summary>
    /// Parse environment variable into a string array, seperated by the `;` character
    /// </summary>
    /// <param name="envKey">Environment Key to search in</param>
    /// <param name="defaultValue">Default return value when null</param>
    /// <returns>Parsed string array</returns>
    private static string[] ParseStringArray(string envKey, string[] defaultValue)
    {
        return (Environment.GetEnvironmentVariable(envKey) ?? string.Join(";", defaultValue)).Trim().Split(";").Where(v => v.Length > 0).ToArray();
    }

    /// <summary>
    /// Parse an environment variable as <see cref="Int32"/>.
    /// 
    /// - Fetch Environment variable (when null, set to <see cref="defaultValue"/> as string)
    /// - Do regex match ^([0-9]+)$
    /// - When success, parse item as integer then return
    /// - When fail, return default value
    /// </summary>
    /// <returns></returns>
    private static int ParseInt(string envKey, int defaultValue)
    {
        var item = Environment.GetEnvironmentVariable(envKey) ?? defaultValue.ToString();
        item = item.Trim();
        var regex = new Regex(@"^([0-9]+)$");
        if (regex.IsMatch(item))
        {
            var match = regex.Match(item);
            var target = match.Groups[1].Value;
            return int.Parse(target);
        }
        Log.Warn($"Failed to parse {envKey} as integer (regex failed. value is \"{item}\"");
        return defaultValue;
    }

    /// <summary>
    /// Key: LOG_COLOR
    /// Default: true
    /// 
    /// Change console text/background color on logging.
    /// </summary>
    public static bool EnableLogColor => ParseBool("LOG_COLOR", true);
    
    /// <summary>
    /// Key: DATA_DIR
    /// Default: ./data/
    /// 
    /// Directory where all file-based data is stored. Used for caching and whatnot.
    /// </summary>
    public static string DataDirectory =>
        ParseString(
            "DATA_DIR", Path.Join(
                Directory.GetCurrentDirectory(),
                "data"));
    /// <summary>
    /// Key: DATA_DIR_FONTCACHE
    /// Default: ./{DataDirectory}/fontcache
    ///
    /// Directory where the font cache will be extracted to
    /// </summary>
    public static string FontCache =>
        ParseString(
            "DATA_DIR_FONTCACHE", Path.Join(
                DataDirectory,
                "fontcache"));
    
    /// <summary>
    /// Key: CONFIG_LOCATION
    /// Default: ./data/config.json
    ///
    /// File location where the config is stored.
    /// </summary>
    public static string ConfigLocation =>
        ParseString(
            "CONFIG_LOCATION", Path.Join(
                DataDirectory, "config.json"));
}