using System.Text.Json.Serialization;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace XeniaBot.WebPanel.Models.BanSyncSearch;

public class BanSyncSearchApiResultV1
{
    [JsonPropertyName("requestedForGuildId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? RequestedForGuildId { get; set; }
    
    [JsonPropertyName("requestedForUserId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? RequestedForUserId { get; set; }
    
    /// <summary>
    /// Total amount of records that can be provided with the query used
    /// </summary>
    [JsonPropertyName("totalRecordCount")]
    public required long TotalRecordCount { get; set; }
    
    /// <summary>
    /// Request query that was used to generate this data.
    /// </summary>
    [JsonPropertyName("requestQuery")]
    public required BanSyncSearchApiQueryDtoV1 RequestQuery { get; set; }
    
    /// <summary>
    /// Searched records
    /// </summary>
    [JsonPropertyName("records")]
    public required BanSyncSearchApiResultRecordV1[] Records { get; set; }
}
