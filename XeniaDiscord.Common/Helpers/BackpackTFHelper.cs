using System.Text.Json;
using XeniaDiscord.Common.ApiModels.BackpackTF;
using XeniaDiscord.Common.Services;
using JObject = Newtonsoft.Json.Linq.JObject;

namespace XeniaDiscord.Common.Helpers;

public static class BackpackTFHelper
{
    public static JsonSerializerOptions SerializerOptions => new JsonSerializerOptions()
    {
        WriteIndented = true,
        IncludeFields = true
    };
    public static T ParseResponse<T>(string jsonContent)
        where T : class, new()
    {
        var json = JObject.Parse(jsonContent);
        if (json.TryGetValue("response", out var responseObj))
        {
            var responseJsonContent = responseObj.ToString();

            var result = JsonSerializer.Deserialize<T>(responseJsonContent, SerializerOptions);
            return result ?? new();
        }
        return new();
    }
    public static BackpackTFCurrencyResult FormatCurrencies(this IReadOnlyList<CurrencyItem> items)
    {
        var result = new BackpackTFCurrencyResult()
        {
            Items = items,
            CostLastUpdatedAt = DateTimeOffset.UtcNow
        };
        foreach (var item in items)
        {
            if (item.Name.Equals("Mann Co. Supply Crate Key", StringComparison.OrdinalIgnoreCase) &&
                item.Price.Currency.Equals("metal", StringComparison.OrdinalIgnoreCase))
            {
                result.RefinedPerKey = item.Price.Value;
            }
            

            if (item.Name.Equals("Refined Metal", StringComparison.OrdinalIgnoreCase) &&
                item.Price.Currency.Equals("usd", StringComparison.OrdinalIgnoreCase))
            {
                result.DollarPerRefined = item.Price.Value;
            }
        }
        return result;
    }
}
