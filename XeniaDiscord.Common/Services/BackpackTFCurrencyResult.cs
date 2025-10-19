using XeniaDiscord.Common.ApiModels.BackpackTF;

namespace XeniaDiscord.Common.Services;

public class BackpackTFCurrencyResult
{
    public decimal RefinedPerKey { get; set; }
    public decimal DollarPerRefined { get; set; }
    public decimal DollarPerKey { get; set; } = 2.5m;

    public IReadOnlyList<CurrencyItem> Items { get; set; } = [];
    public DateTimeOffset CostLastUpdatedAt { get; set; }

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

    public decimal GetRefinedCost(CurrencyPrice price)
    {
        return price.Currency.Trim().ToLower() switch
        {
            "metal" => price.Value,
            "usd" => CalcDollarToRefined(price.Value),
            "keys" => CalcKeyToRefined(price.Value),
            _ => throw new NotImplementedException($"Unexpected currency {price.Currency}")
        };
    }

    public decimal GetKeyCost(CurrencyPrice price)
    {
        return price.Currency.Trim().ToLower() switch
        {
            "metal" => CalcRefinedToKey(price.Value),
            "usd" => CalcDollarToKey(price.Value),
            "keys" => price.Value,
            _ => throw new NotImplementedException($"Unexpected currency {price.Currency}")
        };
    }

    public decimal GetDollarCost(CurrencyPrice price)
    {
        return price.Currency.Trim().ToLower() switch
        {
            "metal" => CalcRefinedToDollar(price.Value),
            "usd" => price.Value,
            "keys" => CalcKeyToDollar(price.Value),
            _ => throw new NotImplementedException($"Unexpected currency {price.Currency}")
        };
    }
}
