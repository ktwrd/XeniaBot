using System.Net;
using XeniaDiscord.Common;
using XeniaDiscord.Common.ApiModels.BackpackTF;
using XeniaDiscord.Common.Helpers;
using XeniaDiscord.Common.Services;

namespace XeniaBot.Common.Services;

public class BackpackTFService : IBackpackTFService
{
    private readonly HttpClient _httpClient;
    public BackpackTFService()
    {
        _httpClient = new();
    }

    public string ApiKey { get; private set; }
    public const string Endpoint = "https://backpack.tf/api";

    public async Task<BackpackTFCurrencyResult?> GetCurrenciesAsync()
    {
        if (string.IsNullOrEmpty(ApiKey))
        {
            throw new InvalidOperationException("API Key cannot be null or empty");
        }
        var url = $"{Endpoint}/IGetCurrencies/v1?key={ApiKey}";
        var response = await _httpClient.GetAsync(url);
        var stringContent = response.Content.ReadAsStringAsync().Result;
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var res = BackpackTFHelper.ParseResponse<CurrencyResponse>(stringContent)
                .Currencies?
                .Select(v => v.Value)
                .ToList();
            return res?.FormatCurrencies();
        }

        return null;
    }
}