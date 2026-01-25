using System.Text.Json.Serialization;

namespace XeniaDiscord.WebApi.Models;

public class HealthResponse : BaseResponse
{
    public HealthResponse()
    {
        Success = true;
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("userId")]
    public ulong UserId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("userDisplayName")]
    public string? UserDisplayName { get; set; }

    [JsonPropertyName("modules")]
    public Dictionary<string, string> Modules { get; set; } = [];
}
