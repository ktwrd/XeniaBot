using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace XeniaBot.Core.Modules;

public partial class AuthentikAdminModule
{
    public async Task<AuthentikPaginationResponse<AuthentikUserResponse>?> GetUsers(string? username = null)
    {
        var url = "core/users/";
        var p = new List<string>()
        {
            "page_size=9999999"
        };
        if (username != null)
            p.Add($"username={WebUtility.UrlEncode(username)}");
        url += "?" + string.Join("&", p);
        var response = await GetAsync(url);
        var stringContent = response.Content.ReadAsStringAsync().Result;
        if (response.StatusCode != HttpStatusCode.OK)
            return null;

        var data = JsonSerializer.Deserialize<AuthentikPaginationResponse<AuthentikUserResponse>>(
            stringContent, Program.SerializerOptions);
        return data;
    }

    public async Task<AuthentikUserResponse?> GetUser(string id)
    {
        var response = await GetAsync($"core/users/{id}/");
        if (response.StatusCode != HttpStatusCode.OK)
            return null;

        var stringContent = response.Content.ReadAsStringAsync().Result;
        var data = JsonSerializer.Deserialize<AuthentikUserResponse>(stringContent, Program.SerializerOptions);
        return data;
    }

    public async Task<bool> DeleteUser(string id)
    {
        var response = await DeleteAsync($"core/users/{id}/");
        return response.StatusCode == HttpStatusCode.NoContent;
    }
}

public class AuthentikPaginationResponse<T>
{
    [JsonPropertyName("pagination")]
    public AuthentikPagination Pagination { get; set; }
    [JsonPropertyName("results")]
    public T[] Results { get; set; }
}

public class AuthentikPagination
{
    [JsonPropertyName("next")]
    public int Next { get; set; }
    [JsonPropertyName("previous")]
    public int Previous { get; set; }
    [JsonPropertyName("count")]
    public int Count { get; set; }
    [JsonPropertyName("current")]
    public int CurrentIndex { get; set; }
    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }
    [JsonPropertyName("start_index")]
    public int StartIndex { get; set; }
    [JsonPropertyName("end_index")]
    public int EndIndex { get; set; }
}
public class AuthentikMinimalUser
{
    [JsonPropertyName("pk")]
    public int Id { get; set; }
    [JsonPropertyName("username")]
    public string Username { get; set; }
    [JsonPropertyName("name")]
    public string DisplayName { get; set; }
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }
    [JsonPropertyName("last_login")]
    public string? LastLogin { get; set; }
    [JsonPropertyName("email")]
    public string Email { get; set; }
    [JsonPropertyName("attributes")]
    public Dictionary<string, object> Attributes { get; set; }
    [JsonPropertyName("uid")]
    public string Uid { get; set; }
}