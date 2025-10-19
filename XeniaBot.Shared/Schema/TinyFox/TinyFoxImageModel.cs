using System.Text.Json.Serialization;

namespace XeniaBot.Shared.Schema.TinyFox;

public class TinyFoxImageModel
{
    [JsonPropertyName("loc")]
    public string Location { get; set; } = "";

    [JsonIgnore]
    public string ImageLocation => $"https://api.tinyfox.dev{Location}";

    [JsonPropertyName("remaining_api_calls")]
    public string RemainingAPICalls { get; set; } = "";
}
