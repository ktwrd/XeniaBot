using System.Text.Json.Serialization;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace XeniaBot.WebPanel.Models.BanSyncSearch;

public class BanSyncSearchApiResultRecordV1
{
    /// <summary>
    /// Unique Record ID, which will match a BanSync record.
    /// </summary>
    [JsonPropertyName("id")]
    public required string RecordId { get; set; }
    
    /// <summary>
    /// Guild ID that the person got banned in
    /// </summary>
    [JsonPropertyName("guildId")]
    public required ulong GuildId { get; set; }
    
    /// <summary>
    /// User ID of the person that got banned
    /// </summary>
    [JsonPropertyName("userId")]
    public required ulong UserId { get; set; }
    
    /// <summary>
    /// Name of the Guild that the person was banned in
    /// </summary>
    [JsonPropertyName("guildName")]
    public required string GuildName { get; set; }
    
    /// <summary>
    /// Username of the person that got banned
    /// </summary>
    [JsonPropertyName("username")]
    public required string Username { get; set; }
    
    /// <summary>
    /// Unix Epoch (seconds)
    /// </summary>
    [JsonPropertyName("createdAt")]
    public required long CreatedAt { get; set; }
    
    /// <summary>
    /// User ID that banned this user
    /// </summary>
    [JsonPropertyName("createdByUserId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public ulong? CreatedByUserId { get; set; }
        
    [JsonPropertyName("reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string? Reason { get; set; }
}