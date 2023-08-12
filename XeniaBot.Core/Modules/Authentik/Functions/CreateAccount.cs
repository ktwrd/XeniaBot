using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace XeniaBot.Core.Modules;

public partial class AuthentikAdminModule
{
    public async Task<AuthentikUserResponse?> CreateAccountAsync(
        string username,
        string path = "users",
        string displayName = "",
        bool active = true,
        string[]? groups = null,
        string email = "",
        Dictionary<string, object>? attributes = null)
    {
        var pushData = new AuthentikCreateUserRequest()
        {
            Username = username,
            Path = path,
            DisplayName = displayName,
            IsActive = active,
            Groups = groups ?? Array.Empty<string>(),
            Email = email,
            Attributes = attributes ?? new Dictionary<string, object>()
        };
        var response = await PostAsync("core/users/", JsonContent.Create(pushData, options: Program.SerializerOptions));
        var stringContent = response.Content.ReadAsStringAsync().Result;
        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.BadRequest)
        {
            throw new Exception(stringContent);
        }
        if ((int)response.StatusCode >= 300 || (int)response.StatusCode < 200)
            return null;

        var data = JsonSerializer.Deserialize<AuthentikUserResponse>(stringContent, Program.SerializerOptions);
        return data;
    }

    public async Task<string?> CreatePasswordResetLink(string userId)
    {
        var response = await GetAsync($"core/users/{userId}/recovery/");
        if (response.StatusCode != HttpStatusCode.OK)
            return null;

        var stringContent = response.Content.ReadAsStringAsync().Result;
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(stringContent, Program.SerializerOptions);
        return data.ContainsKey("link") ? data["link"] : null;
    }
}

public class AuthentikUserResponse
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
    public string LastLogin { get; set; }
    [JsonPropertyName("is_superuser")]
    public bool IsSuperuser { get; set; }
    [JsonPropertyName("groups")]
    public string[] Groups { get; set; }
    [JsonPropertyName("email")]
    public string Email { get; set; }
    [JsonPropertyName("avatar")]
    public string Avatar { get; set; }
    [JsonPropertyName("attributes")]
    public Dictionary<string, object> Attributes { get; set; }
    [JsonPropertyName("uid")]
    public string AccountUid { get; set; }
    [JsonPropertyName("path")]
    public string Path { get; set; }
    
    [JsonPropertyName("groups_obj")]
    public AuthentikUserGroup[] GroupsObj { get; set; }
}

public class AuthentikUserGroup
{
    [JsonPropertyName("pk")]
    public string Uuid { get; set; }
    [JsonPropertyName("num_pk")]
    public int Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("is_superuser")]
    public bool IsSuperuser { get; set; }
    [JsonPropertyName("parent")]
    public string? ParentUuid { get; set; }
    [JsonPropertyName("parent_name")]
    public string? ParentName { get; set; }
    [JsonPropertyName("attributes")]
    public Dictionary<string, object> Attributes { get; set; }
}

public class AuthentikCreateUserRequest
{
    [JsonPropertyName("attributes")]
    public Dictionary<string, object> Attributes { get; set; }
    [JsonPropertyName("email")]
    public string Email { get; set; }
    [JsonPropertyName("groups")]
    public string[] Groups { get; set; }
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }
    [JsonPropertyName("name")]
    public string DisplayName { get; set; }
    [JsonPropertyName("path")]
    public string Path { get; set; }
    [JsonPropertyName("username")]
    public string Username { get; set; }
}