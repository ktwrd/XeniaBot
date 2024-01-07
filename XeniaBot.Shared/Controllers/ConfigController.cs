using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using kate.shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.Shared.Controllers;

public class ConfigController
{
    public ConfigController(IServiceProvider services)
    {
        Data = FetchConfig(services.GetRequiredService<ProgramDetails>());
    }

    public ConfigController(ProgramDetails details)
    {
        Data = FetchConfig(details);
        if (!FeatureFlags.ConfigReadOnly)
        {
            Write(Data);
        }
    }
    #region Read/Write
    public void Write(ConfigData? configData)
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

    public string FetchConfigContent()
    {
        // Check if config is set from environment
        if (FeatureFlags.ConfigFromEnvironment)
        {
            return FetchEnvConfig();
        }
        else
        {
            return FetchFileConfig();
        }
    }
    
    public ConfigData Data { get; private set; }

    public ConfigData FetchConfig(ProgramDetails details)
    {
        var stringContent = FetchConfigContent();

        var obj = JObject.Parse(stringContent);
        var v = obj["Version"]?.ToString();
        switch (obj["Version"]?.ToString() ?? "")
        {
            case "":
            case "1":
                var oldConf = JsonSerializer.Deserialize<ConfigDataV1>(stringContent, SerializerOptions);
                ValidateConfigV1(oldConf, details);
                break;
        }
        
        var config = new ConfigData();
        try
        {
            config = ConfigData.Migrate(stringContent);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to parse ConfigData", ex);
            throw new Exception("Failed to parse ConfigData", ex);
        }

        ConfigData.Validate(details, config);

        return config;
    }

    public void ValidateConfigV1(ConfigDataV1 config, ProgramDetails details)
    {
        var defaultData = JsonSerializer.Deserialize<Dictionary<string, object>>(
            JsonSerializer.Serialize(new ConfigDataV1(), SerializerOptions), SerializerOptions) ?? new Dictionary<string, object>();

        var keysToValidate = ConfigDataV1.RequiredKeys.ToList();
        if (details.Platform == XeniaPlatform.Bot)
            keysToValidate = keysToValidate.Concat(ConfigDataV1.RequiredBotKeys).ToList();
        else if (details.Platform == XeniaPlatform.WebPanel)
            keysToValidate = keysToValidate.Concat(ConfigDataV1.RequiredDashKeys).ToList();
        
        var (baseValidateMissing, baseValidateNotChanged) = ValidateConfigKeysV1(config, defaultData, keysToValidate.ToArray());
        if (baseValidateMissing > 0 || baseValidateNotChanged > 0)
        {
            Log.Error("There are multiple issues with your config file. Please resolve them.");
            Environment.Exit(11);
        }
    }

    private (int, int) ValidateConfigKeysV1(ConfigDataV1 source, Dictionary<string, object> clean, string[] keys)
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

            if (clean.TryGetValue(i, out var v))
            {
                if (sourceDict[i] == v)
                {
                    notChanged.Add(i);
                    Log.Warn($"source[{i}] not changed");
                }
            }
        }
        
        if (missing.Count > 0)
            Log.Error($"There are {missing.Count} item");
        if (notChanged.Count > 0)
            Log.Error($"{notChanged.Count} item{XeniaHelper.Pluralize(missing.Count)} that haven't changed");
        return (missing.Count, notChanged.Count);
    }

    public static JsonSerializerOptions SerializerOptions =>
        new JsonSerializerOptions()
        {
            IncludeFields = true,
            WriteIndented = true
        };
    
    private string FetchFileConfig()
    {
        var data = new ConfigData();
        if (!File.Exists(FeatureFlags.ConfigLocation))
        {
            File.WriteAllText(FeatureFlags.ConfigLocation, JsonSerializer.Serialize(data, SerializerOptions));
        }

        return File.ReadAllText(FeatureFlags.ConfigLocation);
    }

    private string FetchEnvConfig()
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
                throw new Exception("Failed to decode envconfig from Base64 to String", ex);
            }
        }

        return content ?? "{}";
    }
}