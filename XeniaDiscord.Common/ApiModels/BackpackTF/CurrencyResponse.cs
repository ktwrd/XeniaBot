using System.Text.Json.Serialization;

namespace XeniaDiscord.Common.ApiModels.BackpackTF;

public class CurrencyResponse : BaseResponse
{
    [JsonPropertyName("currencies")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, CurrencyItem>? Currencies { get; set; }
}
