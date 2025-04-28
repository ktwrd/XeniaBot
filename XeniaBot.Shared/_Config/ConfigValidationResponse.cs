using System;
using XeniaBot.Shared.Services;

namespace XeniaBot.Shared;

public class ConfigValidationResponse
{
    /// <summary>
    /// Array of keys which haven't been changed from the default values.
    /// </summary>
    public string[] UnchangedKeys = Array.Empty<string>();
    /// <summary>
    /// Array of keys which haven't been changed from the default value. Includes any keys in 
    /// </summary>
    public string[] UnchangedKeysNoIgnore = Array.Empty<string>();
    /// <summary>
    /// Array of keys which are missing from the provided config in <see cref="ConfigService.Validate(Config)"/>
    /// </summary>
    public string[] MissingKeys = Array.Empty<string>();
    public bool Failure => UnchangedKeys.Length > 0 && MissingKeys.Length > 0;
    public int FailureCount => UnchangedKeys.Length + MissingKeys.Length;
}
