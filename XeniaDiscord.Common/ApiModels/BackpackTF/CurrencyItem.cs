using System.Text.Json.Serialization;

namespace XeniaDiscord.Common.ApiModels.BackpackTF;

public class CurrencyItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("quality")]
    public int Quality { get; set; }

    [JsonPropertyName("priceindex")]
    public string PriceIndex { get; set; } = "";

    [JsonPropertyName("single")]
    public string Single { get; set; } = "";

    [JsonPropertyName("plural")]
    public string Plural { get; set; } = "";

    [JsonPropertyName("round")]
    public int Round { get; set; }

    [JsonPropertyName("blanket")]
    public int Blanket { get; set; }

    [JsonPropertyName("craftable")]
    public string Craftable { get; set; } = "";

    [JsonPropertyName("tradable")]
    public string Tradable { get; set; } = "";

    [JsonPropertyName("defindex")]
    public int DefinedIndex { get; set; }

    [JsonPropertyName("price")]
    public CurrencyPrice Price { get; set; } = "";
}
