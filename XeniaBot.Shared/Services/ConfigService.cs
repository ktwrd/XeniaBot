using kate.shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.Shared.Services;

public class ConfigService
{
    private static readonly Logger Log = LogManager.GetLogger("Xenia.ConfigService");
    public ConfigService(IServiceProvider services)
    {
        Data = FetchConfig(services.GetRequiredService<ProgramDetails>());
    }

    public ConfigService(ProgramDetails details)
    {
        Data = FetchConfig(details);
        if (!FeatureFlags.ConfigReadOnly)
        {
            Write();
        }
    }
    public ConfigData Data { get; private set; }

    #region Read/Write
    public void Write()
    {
        if (!FeatureFlags.ConfigReadOnly)
        {
            Write(Data);
        }
    }
    private static void Write(ConfigData? configData)
    {
        if (FeatureFlags.ConfigReadOnly)
        {
            Log.Warn("Not writing config since FeatureFlags.ConfigReadOnly is true");
            return;
        }
        if (configData == null)
        {
            Log.Error($"Parameter \"configData\" is null.");
        }
        else
        {
            var content = JsonSerializer.Serialize(configData, XeniaHelper.SerializerOptions);
            File.WriteAllText(FeatureFlags.ConfigLocation, content);                
        }
    }
    #endregion

    private static string FetchConfigContent()
    {
        // Check if config is set from environment
        if (FeatureFlags.ConfigFromEnvironment)
        {
            return GetConfigFromEnvironment();
        }
        else
        {
            return GetConfigFromFile();
        }
    }

    private static ConfigData FetchConfig(ProgramDetails details)
    {
        var stringContent = FetchConfigContent();

        var minimal = JsonSerializer.Deserialize<MinimalConfigData>(stringContent);
        switch (minimal?.Version?.ToString()?.Trim())
        {
            case "":
            case "1":
                ValidateConfigV1(
                    JsonSerializer.Deserialize<ConfigDataV1>(stringContent, SerializerOptions) ?? new(),
                    details);
                break;
        }

        ConfigData config;
        try
        {
            config = ConfigData.Migrate(stringContent);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to parse ConfigData");
            throw new InvalidOperationException("Failed to parse ConfigData", ex);
        }

        ConfigData.Validate(details, config);

        return config;
    }

    private static void ValidateConfigV1(ConfigDataV1 config, ProgramDetails details)
    {
        var defaultData = DefaultDictionaryConfigDataV1();

        var keysToValidate = ConfigDataV1.RequiredKeys.ToList();
        if (details.Platform == XeniaPlatform.Bot)
            keysToValidate.AddRange(ConfigDataV1.RequiredBotKeys);
        else if (details.Platform == XeniaPlatform.WebPanel)
            keysToValidate.AddRange(ConfigDataV1.RequiredDashKeys);
        
        var (baseValidateMissing, baseValidateNotChanged) = ValidateConfigKeysV1(config, defaultData, [..keysToValidate]);
        if (baseValidateMissing > 0 || baseValidateNotChanged > 0)
        {
            Log.Error("There are multiple issues with your config file. Please resolve them.");
            Environment.Exit(11);
        }
    }
    private static Dictionary<string, object> DefaultDictionaryConfigDataV1()
    {
        var json = JsonSerializer.Serialize(new ConfigDataV1(), SerializerOptions);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json, SerializerOptions)
            ?? new();
    }

    private static ValidateConfigKeysV1Result ValidateConfigKeysV1(ConfigDataV1 source, Dictionary<string, object> clean, string[] keys)
    {
        var missing = new List<string>();
        var notChanged = new List<string>();
        var sourceDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
            JsonSerializer.Serialize(source, SerializerOptions), SerializerOptions) ?? new Dictionary<string, object>();
        foreach (var i in keys)
        {
            if (!sourceDict.ContainsKey(i))
            {
                missing.Add(i);
                Log.Warn($"source[{i}] not found");
                continue;
            }

            if (clean.TryGetValue(i, out var v) &&
                sourceDict[i] == v)
            {
                notChanged.Add(i);
                Log.Warn($"source[{i}] not changed");
            }
        }
        
        if (missing.Count > 0)
            Log.Error($"Missing {missing.Count} item{XeniaHelper.Pluralize(missing.Count)}");
        if (notChanged.Count > 0)
            Log.Error($"{notChanged.Count} item{XeniaHelper.Pluralize(missing.Count)} that haven't changed\nKeys: " + string.Join(", ", notChanged));
        return new(missing.Count, notChanged.Count);
    }

    private sealed record ValidateConfigKeysV1Result(int Missing, int NotChanged);

    public static readonly JsonSerializerOptions SerializerOptions =
        new()
        {
            IncludeFields = true,
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.Preserve
        };
    
    private static string GetConfigFromFile()
    {
        var data = new ConfigData();
        if (!File.Exists(FeatureFlags.ConfigLocation))
        {
            File.WriteAllText(FeatureFlags.ConfigLocation, JsonSerializer.Serialize(data, SerializerOptions));
        }

        return File.ReadAllText(FeatureFlags.ConfigLocation);
    }

    private static string GetConfigFromEnvironment()
    {
        var content = FeatureFlags.ConfigContent;
        if (FeatureFlags.ConfigContentIsBase64)
        {
            try
            {
                content = GeneralHelper.Base64Decode(content);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to decode envconfig from Base64 to String\nContent: {content}", ex);
            }
        }

        return content ?? "{}";
    }
}