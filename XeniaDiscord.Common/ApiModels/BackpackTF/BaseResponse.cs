using System.Text.Json.Serialization;

namespace XeniaDiscord.Common.ApiModels.BackpackTF;

public class BaseResponse
{
    [JsonPropertyName("success")]
    public int? Success { get; set; }

    [JsonPropertyName("message")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("name")]
    public string? AppName { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
