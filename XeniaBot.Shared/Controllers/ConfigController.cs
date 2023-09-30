using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.Shared.Controllers;

public class ConfigController
{
    public ConfigController()
    {
    }

    public ConfigController(IServiceProvider services)
        : this()
    {
    }
    #region Read/Write
        public ConfigData Read()
        {
            var config = new ConfigData();
            if (File.Exists(Location))
            {
                var content = File.ReadAllText(Location);
                config = JsonSerializer.Deserialize<ConfigData>(content, XeniaHelper.SerializerOptions);
                var validationResponse = Validate(content);
                if (validationResponse.Failure)
                {
                    Log.WriteLine($"Failed to validate config. Encountered {validationResponse.FailureCount} errors.");
                    config = null;
                }

                if (config == null)
                {
                    Log.Error($"Failed to parse config");
                    Log.Error(content);
                    Environment.Exit(1);

                    // This is only here to make VS2022 shut up
                    return new ConfigData();
                }
            }
            else
            {
                Log.Debug("Created config, please populate.");
                Write(config);
            }
            Write(config);
            return config;
        }
        public void Write(ConfigData? configData)
        {
            if (configData == null)
            {
                Log.Error($"Parameter \"configData\" is null.");
            }
            else
            {
                var content = JsonSerializer.Serialize(configData, XeniaHelper.SerializerOptions);
                File.WriteAllText(Location, content);                
            }
        }
        #endregion

        #region Config Validation
        public static string[] IgnoredValidationKeys => ConfigData.IgnoredValidationKeys;
        public ConfigValidationResponse Validate(Dictionary<string, object> configDict)
        {
            Dictionary<string, object> defaultDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(new ConfigData(), XeniaHelper.SerializerOptions),
                XeniaHelper.SerializerOptions) ?? new Dictionary<string, object>();

            var unchangedNoIgnore = new List<string>();
            var missing = new List<string>();

            foreach (var defaultPair in defaultDict)
            {
                bool contains = configDict.ContainsKey(defaultPair.Key);
                bool ignore = IgnoredValidationKeys.Contains(defaultPair.Key);
                if (!contains)
                {
                    Log.Warn($"Missing key \"{defaultPair.Key}\"");
                    missing.Add(defaultPair.Key);
                    continue;
                }

                if (configDict[defaultPair.Key] == defaultPair.Value)
                {
                    unchangedNoIgnore.Add(defaultPair.Key);
                    Log.Error($"Config Key \"{defaultPair.Key}\" has not been changed!");
                }
            }
            var unchanged = unchangedNoIgnore.Where(v => !IgnoredValidationKeys.Contains(v));

            return new ConfigValidationResponse()
            {
                UnchangedKeys = unchanged.ToArray(),
                UnchangedKeysNoIgnore = unchangedNoIgnore.ToArray(),
                MissingKeys = missing.ToArray()
            };
        }
        public ConfigValidationResponse Validate(string fileContent) => Validate(JsonSerializer.Deserialize<Dictionary<string, object>>(fileContent, XeniaHelper.SerializerOptions) ?? new Dictionary<string, object>());
        public ConfigValidationResponse Validate(ConfigData configData) => Validate(JsonSerializer.Serialize(configData, XeniaHelper.SerializerOptions) ?? "{}");
        #endregion

        public string Location => FeatureFlags.ConfigLocation;
}