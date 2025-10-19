using System.Text.Json.Serialization;

namespace XeniaDiscord.Common.ApiModels.BackpackTF;

public class CurrencyPrice
{
    [JsonPropertyName("value")]
    public decimal Value { get; set; }

    [JsonRequired]
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "";

    [JsonPropertyName("difference")]
    public float Difference { get; set; }

    [JsonPropertyName("last_update")]
    public long LastUpdateTimestamp { get; set; }

    [JsonPropertyName("value_high")]
    public decimal ValueHighpoint { get; set; }

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Currency.Trim()))
        {
            return Value.ToString();
        }
        return Currency.Trim().ToLower() switch
        {
            "usd" => $"US${Math.Round(Value, 2)}",
            "metal" => $"{Math.Round(Value, 2)}ref",
            "keys" => Math.Round(Value, 2) + (Value > 1 ? " keys" : " key"),
            _ => throw new NotImplementedException($"Currency \"{Currency}\" not implemented")
        };
    }
}
