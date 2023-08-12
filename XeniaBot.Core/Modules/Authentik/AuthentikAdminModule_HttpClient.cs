using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XeniaBot.Core.Modules;

public partial class AuthentikAdminModule
{
    public HttpClient _http => new HttpClient();

    public void InitHttpClient()
    {
        if (!_http.DefaultRequestHeaders.Contains("authorization"))
        {
            _http.DefaultRequestHeaders.Add("authorization", $"Bearer {Program.ConfigData.AuthentikToken}");
        }
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Program.ConfigData.AuthentikToken);
    }
    private void ThrowOnFailure(HttpResponseMessage message)
    {
        var failureList = new HttpStatusCode[]
        {
            HttpStatusCode.Forbidden
        };
        if (failureList.Contains(message.StatusCode))
        {
            var stringContent = message.Content.ReadAsStringAsync().Result;
            switch (message.StatusCode)
            {
                case HttpStatusCode.Forbidden:
                    var forbiddenDeser = JsonSerializer.Deserialize<AuthentikGenericAPIError>(
                        stringContent, Program.SerializerOptions);
                    throw new AuthentikException(forbiddenDeser);
                    break;
            }
        }
    }
    public async Task<string?> SafelyGetUserId(string param)
    {
        var integerRegex = new Regex(@"^[0-9]+$");
        if (!integerRegex.IsMatch(param))
        {
            var searchResponse = await GetUsers(param);
            if (searchResponse.Results.Length < 1)
            {
                return null;
            }
            return searchResponse.Results[0].Id.ToString();
        }

        return param;
    }
    public async Task<string?> SafelyGetGroupId(string param)
    {
        var integerRegex = new Regex(@"^[0-9]+$");
        if (!integerRegex.IsMatch(param))
        {
            var searchResponse = await GetGroups(param);
            if (searchResponse.Results.Length < 1)
            {
                return null;
            }
            return searchResponse.Results[0].Uuid;
        }

        return param;
    }
    private HttpRequestMessage GetBaseSend(string url, HttpMethod method, HttpContent? content)
    {
        InitHttpClient();
        return new HttpRequestMessage()
        {
            RequestUri = new Uri($"https://{Program.ConfigData.AuthentikUrl}/api/v3/{url}"),
            Headers =
            {
                {
                    "authorization", $"Bearer {Program.ConfigData.AuthentikToken}"
                },
                {
                    "accept", "application/json"
                }
            },
            Method = method,
            Content = content
        };
    }
    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        InitHttpClient();
        var data = GetBaseSend(url, HttpMethod.Get, null);
        var res = await _http.SendAsync(data);
        ThrowOnFailure(res);
        return res;
    }

    public async Task<HttpResponseMessage> PostAsync(string url, HttpContent content)
    {
        InitHttpClient();
        var data = GetBaseSend(url, HttpMethod.Post, content);
        var res = await _http.SendAsync(data);
        ThrowOnFailure(res);
        return res;
    }

    public async Task<HttpResponseMessage> PutAsync(string url, HttpContent content)
    {
        InitHttpClient();
        var data = GetBaseSend(url, HttpMethod.Put, content);
        var res = await _http.SendAsync(data);
        ThrowOnFailure(res);
        return res;
    }

    public async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        InitHttpClient();
        var data = GetBaseSend(url, HttpMethod.Delete, null);
        var res = await _http.SendAsync(data);
        ThrowOnFailure(res);
        return res;
    }
}
public class AuthentikGenericAPIError
{
    [JsonPropertyName("detail")]
    public string Detail { get; set; }
    [JsonPropertyName("code")]
    public string? ErrorCode { get; set; }
}