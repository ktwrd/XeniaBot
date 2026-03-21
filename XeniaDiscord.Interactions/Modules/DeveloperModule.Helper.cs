using Discord;
using System.Text;
using System.Text.Json;

namespace XeniaDiscord.Interactions.Modules;

partial class DeveloperModule
{
    private static FileAttachment CreateAttachment<TData>(string filename, TData data)
        where TData : class
    {
        var json = JsonSerializer.Serialize(data, jsonSerializerOptions);
        return new FileAttachment(
                    new MemoryStream(Encoding.UTF8.GetBytes(json ?? "null")),
                    fileName: filename);
    }
    private static FileAttachment CreateStringAttachment(string filename, string content)
    {
        return new FileAttachment(
                    new MemoryStream(Encoding.UTF8.GetBytes(content)),
                    fileName: filename);
    }
    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = true,
        IgnoreReadOnlyFields = false,
        IgnoreReadOnlyProperties = false,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
    };
}
