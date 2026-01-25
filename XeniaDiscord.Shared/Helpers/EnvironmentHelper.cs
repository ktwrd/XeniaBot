using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace XeniaBot.Shared.Helpers;

/// <summary>
/// Helper class for environment variables.
/// </summary>
public static class EnvironmentHelper
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    #region Parsing
    public const string EnvLocationImportKey = "CUSTOM_ENV_LOCATION";
    private static Dictionary<string, string>? ParsedEnvData = null;
    private static void ParseEnvFile(bool force = false)
    {
        if (!force && ParsedEnvData != null)
            return;
        var location = Environment.GetEnvironmentVariable(EnvLocationImportKey);
        if (location == null)
        {
            ParsedEnvData = new();
            return;
        }

        ParsedEnvData = new();
        ParseEnvContent(File.ReadAllLines(location!));
    }

    private static void ParseEnvData()
    {
        if (ParsedEnvData != null)
            return;
        if (Environment.GetEnvironmentVariable(EnvLocationImportKey) != null)
        {
            ParseEnvFile(true);
        }
    }

    private static void ParseEnvContent(string[] content)
    {
        ParsedEnvData ??= new();
        foreach (var line in content)
        {
            var si = line.IndexOf('=');
            if (si == -1)
                continue;

            var lci = line.IndexOf('#');
            if (lci < si && lci != -1)
                continue;

            var key = line[..si];
            var value = line[(si + 1)..];

            var fq = value.IndexOf('"');
            var lq = value.LastIndexOf('"');


            // if value is surrounded in quotes, then remove them.
            if (fq != -1 && lq != -1 && fq < lq)
            {
                value = value.Substring(fq + 1, lq - 1);
            }
            else
            {
                // when there is a comment char, remove everything after it (including the comment char)
                var vci = value.IndexOf('#');
                if (vci != -1)
                {
                    value = value[..^(vci + 1)];
                }
            }

            ParsedEnvData[key] = value;
        }
    }

    private static string? GetValue(string key)
    {
        ParseEnvData();

        if (ParsedEnvData?.TryGetValue(key, out var s) ?? false)
        {
            return s;
        }
        else
        {
            return Environment.GetEnvironmentVariable(key);
        }
    }

    /// <summary>
    /// Parses an environment variable as a boolean.
    /// When the environment variable isn't found, it wil default to <paramref name="defaultValue"/>
    /// </summary>
    /// <param name="environmentKey">Name of the environment variable.</param>
    /// <param name="defaultValue">Default value when no value could be parsed, or the environment variable isn't set.</param>
    /// <returns>
    /// Will return:
    /// <list type="bullet">
    /// <item>If <see cref="bool.TryParse(string, out bool)"/> is successful, then the result parameter is returned.</item>
    /// <item>If <see cref="int.TryParse(string, out int)"/> is successful and the result is <c>&gt;0</c>, then <see langword="true"/> is returned. Otherwise <see langword="false"/> is returned.</item>
    /// <item>If equal to (<see cref="StringComparison.InvariantCultureIgnoreCase"/>) <c>yes</c> or <c>y</c>, then <see langword="true"/> is returned.</item>
    /// <item>If equal to (<see cref="StringComparison.InvariantCultureIgnoreCase"/>) <c>no</c> or <c>n</c>, then <see langword="false"/> is returned.</item>
    /// </list>
    /// If none of those conditions are met, then <paramref name="defaultValue"/> is returned.
    /// </returns>
    public static bool ParseBool(string environmentKey, bool defaultValue)
    {
        var value = GetValue(environmentKey)
                   ?? $"{defaultValue}";
        if (int.TryParse(value, out var intValue))
            return intValue > 0;
        if (bool.TryParse(value, out var boolValue))
            return boolValue;

        return value.Trim().ToLower() switch
        {
            "yes" => true,
            "y" => true,
            "no" => false,
            "n" => false,
            _ => defaultValue
        };
    }

    /// <summary>
    /// Just <see cref="Environment.GetEnvironmentVariable(string)"/> but when <see langword="null"/> it's <paramref name="defaultValue"/>
    /// </summary>
    /// <param name="name">Name of the Environment Variable to get the value for.</param>
    /// <param name="defaultValue">Default Value to be returned when the environment variable isn't set,
    /// or when this method failed to parse it,
    /// or when it's null &amp; empty and <paramref name="defaultWhenNullOrEmpty"/> is set to <see langword="true"/>.</param>
    /// <param name="defaultWhenNullOrEmpty">
    /// <para>When the environment variable is null or empty (via <see cref="string.IsNullOrEmpty"/>), then just pretend it's not set and use the default value.</para>
    /// Default value: <see langword="false"/>
    /// </param>
    public static string ParseString(string name, string defaultValue, bool defaultWhenNullOrEmpty = false)
    {
        var value = GetValue(name);
        if (defaultWhenNullOrEmpty && string.IsNullOrEmpty(value))
        {
            value = null;
        }
        return value ?? defaultValue;
    }

    /// <summary>
    /// Parse environment variable into a string array, seperated by the <paramref name="delimiter"/> provided.
    /// </summary>
    /// <param name="name">Name of the Environment Variable to get the value for.</param>
    /// <param name="defaultValue">Default Value to be returned when the environment variable isn't set,
    /// or when this method failed to parse it,
    /// or when it's null &amp; empty and <paramref name="defaultWhenNullOrEmpty"/> is set to <see langword="true"/>.</param>
    /// <param name="delimiter">Delimiter to split the values by. Default value: <c>;</c></param>
    /// <param name="defaultWhenNullOrEmpty">
    /// <para>When the environment variable is null or empty (via <see cref="string.IsNullOrEmpty"/>), then just pretend it's not set and use the default value.</para>
    /// Default value: <see langword="false"/>
    /// </param>
    /// <param name="ignoreEmptyValues">
    /// <para>When <see langword="true"/>, only keep items in the parsed array if they're not null or empty (via <see cref="string.IsNullOrEmpty"/>)</para>
    /// Default value: <see langword="true"/>
    /// </param>
    /// <returns>Parsed string array</returns>
    public static IReadOnlyList<string> ParseStringArray(string name, string[] defaultValue, string delimiter = ";", bool defaultWhenNullOrEmpty = false, bool ignoreEmptyValues = true)
    {
        var value = GetValue(name);
        if (value == null || (defaultWhenNullOrEmpty && string.IsNullOrEmpty(value)))
            return defaultValue;

        var split = value.Split(delimiter);
        var result = split;
        if (ignoreEmptyValues)
        {
            result = result.Where(e => !string.IsNullOrEmpty(e)).ToArray();
        }
        return result;
    }

    /// <summary>
    /// <para>Parse an environment variable as <see cref="Int32"/>.</para>
    ///
    /// Uses <see cref="int.TryParse(string, out int)"/> to parse the value. If that fails, then <paramref name="defaultValue"/> is returned.
    /// </summary>
    /// <param name="name">Name of the Environment Variable to get the value for.</param>
    /// <param name="defaultValue">Default Value to be returned when the environment variable isn't set, or this method failed to parse it.</param>
    public static int ParseInt(string name, int defaultValue)
    {
        var rawValue = GetValue(name);
        var value = rawValue ?? defaultValue.ToString();
        if (int.TryParse(value, out var result))
        {
            return result;
        }
        if (!string.IsNullOrEmpty(rawValue) && rawValue != defaultValue.ToString())
        {
            Log.Warn($"Failed to parse value \"{value}\" as integer for environment variable \"{name}\"");
        }
        return defaultValue;
    }
    #endregion
}
