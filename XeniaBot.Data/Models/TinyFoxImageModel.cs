using System.Text.Json.Serialization;

namespace XeniaBot.Data.Models;

public class TinyFoxImageModel
{
    [JsonPropertyName("loc")]
    public string Location { get; set; } = "";

    public string ImageLocation => $"https://api.tinyfox.dev{Location}";

    [JsonPropertyName("remaining_api_calls")]
    public string RemainingAPICalls { get; set; } = "";
}
