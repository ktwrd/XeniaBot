﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using XeniaBot.Shared;

namespace XeniaBot.Core.Services.Wrappers;

[XeniaController]
public class BackpackTFService : BaseService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public BackpackTFService(IServiceProvider services)
        : base(services)
    {
        _http = new HttpClient();
        var conf = services.GetRequiredService<ConfigData>();
        _apiKey = conf.ApiKeys.BackpackTF ?? "";
    }

    public T ParseResponse<T>(string content)
    {
        var obj = JObject.Parse(content);
        var item = obj["response"];
        var itemStr = item?.ToString();
        return (T)JsonSerializer.Deserialize(itemStr, typeof(T), Program.SerializerOptions);
    }

    public async Task<ICollection<BackpackCurrencyItem>?> GetCurrenciesAsync()
    {
        var url = $"https://backpack.tf/api/IGetCurrencies/v1?key={_apiKey}";
        if (_apiKey?.Length < 1)
            throw new Exception("API Key not provided!");
        var response = await _http.GetAsync(url);
        var stringContent = response.Content.ReadAsStringAsync().Result;
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var res = ParseResponse<BackpackCurrencyResponse>(stringContent).Currencies.Select(v => v.Value).ToList();
            if (res != null)
                FormatCurrencies(res);
            return res;
        }

        return null;
    }
    public decimal RefinedPerKey { get; private set; }
    public decimal DollarPerRefined { get; private set; }
    public DateTimeOffset CostLastUpdatedAt { get; private set; }
    public const decimal DollarPerKey = 2.5m;
    public void FormatCurrencies(ICollection<BackpackCurrencyItem> currencyItems)
    {
        foreach (var item in currencyItems)
        {
            if (item.Name == "Mann Co. Supply Crate Key")
            {
                if (item.Price.Currency == "metal")
                {
                    RefinedPerKey = item.Price.Value;
                }
            }

            if (item.Name == "Refined Metal")
            {
                if (item.Price.Currency == "usd")
                {
                    DollarPerRefined = item.Price.Value;
                }
            }
        }
        CostLastUpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Convert refined to USD based off <see cref="RefinedPerKey"/>.
    /// </summary>
    /// <param name="refined">Amount of refined</param>
    /// <returns>USD that the refined specified is worth.</returns>
    public decimal CalcRefinedToDollar(decimal refined)
    {
        return DollarPerRefined * refined;
    }

    /// <summary>
    /// Converts USD to Refined, depends on <see cref="RefinedPerKey"/>
    /// </summary>
    public decimal CalcDollarToRefined(decimal dollar)
    {
        return dollar / DollarPerRefined;
    }

    public decimal CalcDollarToKey(decimal dollar)
    {
        return dollar / DollarPerKey;
    }

    public decimal CalcKeyToDollar(decimal key)
    {
        return key * DollarPerKey;
    }

    public decimal CalcRefinedToKey(decimal refined)
    {
        return refined / RefinedPerKey;
    }

    public decimal CalcKeyToRefined(decimal keys) => keys * RefinedPerKey;
    
    public decimal GetRefinedCost(BackpackCurrencyPrice price)
    {
        switch (price.Currency)
        {
            case "metal":
                return price.Value;
            case "usd":
                return CalcDollarToRefined(price.Value);
            case "keys":
                return CalcKeyToRefined(price.Value);
            default:
                throw new NotImplementedException($"Unexpected currency {price.Currency}");
        }
    }

    public decimal GetKeyCost(BackpackCurrencyPrice price)
    {
        switch (price.Currency)
        {
            case "metal":
                return CalcRefinedToKey(price.Value);
            case "usd":
                return CalcDollarToKey(price.Value);
            case "keys":
                return price.Value;
            default:
                throw new NotImplementedException($"Unexpected currency {price.Currency}");
        }
    }

    public decimal GetDollarCost(BackpackCurrencyPrice price)
    {
        switch (price.Currency)
        {
            case "metal":
                return CalcRefinedToDollar(price.Value);
            case "usd":
                return price.Value;
            case "keys":
                return CalcKeyToDollar(price.Value);
            default:
                throw new NotImplementedException($"Unexpected currency {price.Currency}");
        }
    }
}

public class BaseBackpackResponse
{
    [JsonPropertyName("success")]
    public int Success { get; set; }
    [JsonPropertyName("message")]
    public string? ErrorMessage { get; set; }
    [JsonPropertyName("name")]
    public string? AppName { get; set; }
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public class BackpackCurrencyResponse : BaseBackpackResponse
{
    [JsonPropertyName("currencies")]
    public Dictionary<string, BackpackCurrencyItem> Currencies { get; set; }
}

public class BackpackCurrencyItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("quality")]
    public int Quality { get; set; }
    [JsonPropertyName("priceindex")]
    public string PriceIndex { get; set; }
    [JsonPropertyName("single")]
    public string Single { get; set; }
    [JsonPropertyName("plural")]
    public string Plural { get; set; }
    [JsonPropertyName("round")]
    public int Round { get; set; }
    [JsonPropertyName("blanket")]
    public int Blanket { get; set; }
    [JsonPropertyName("craftable")]
    public string Craftable { get; set; }
    [JsonPropertyName("tradable")]
    public string Tradable { get; set; }
    [JsonPropertyName("defindex")]
    public int DefinedIndex { get; set; }
    [JsonPropertyName("price")]
    public BackpackCurrencyPrice Price { get; set; }
}

public class BackpackCurrencyPrice
{
    [JsonPropertyName("value")]
    public decimal Value { get; set; }
    [JsonPropertyName("currency")]
    public string Currency { get; set; }
    [JsonPropertyName("difference")]
    public float Difference { get; set; }
    [JsonPropertyName("last_update")]
    public long LastUpdateTimestamp { get; set; }
    [JsonPropertyName("value_high")]
    public decimal ValueHighpoint { get; set; }

    public override string ToString()
    {
        switch (Currency)
        {
            case "usd":
                return $"US${Math.Round(Value, 2)}";
            case "metal":
                return $"{Math.Round(Value, 2)}ref";
            case "keys":
                return Math.Round(Value, 2) + (Value > 1 ? " keys" : " key");
            default:
                throw new NotImplementedException($"Currency {Currency} not implemented");
        }
    }
}