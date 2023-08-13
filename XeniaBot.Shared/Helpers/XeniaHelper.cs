using System.Text.Json;

namespace XeniaBot.Shared.Helpers;

public static class XeniaHelper
{
    public static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions()
    {
        IgnoreReadOnlyFields = true,
        IgnoreReadOnlyProperties = true,
        IncludeFields = true,
        WriteIndented = true
    };
}