using System.Text.Json.Serialization;

namespace XeniaBot.WebPanel.Models;

public class NotFoundApiResponse
{
    [JsonPropertyName("success")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public bool Success => false;
    
    [JsonPropertyName("message")]
    public required string Message { get; set; }
}