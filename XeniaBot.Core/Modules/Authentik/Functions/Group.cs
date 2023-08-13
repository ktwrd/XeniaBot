using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;

namespace XeniaBot.Core.Modules;

public partial class AuthentikAdminModule
{
    public async Task<bool> AddToGroup(int userId, string groupUuid)
    {
        var response = await PostAsync(
            $"core/groups/{groupUuid}/add_user/?pk={userId}", JsonContent.Create(
                new AuthentikGroupUserModifyRequest()
                {
                    UserId = userId
                }, options: Program.SerializerOptions));
        return response.StatusCode == HttpStatusCode.NoContent;
    }

    public async Task<bool> RemoveFromGroup(int userId, string groupUuid)
    {
        var response = await PostAsync(
            $"core/groups/{groupUuid}/remove_user/?pk={userId}", JsonContent.Create(
                new AuthentikGroupUserModifyRequest()
                {
                    UserId = userId
                }, options: Program.SerializerOptions));
        return response.StatusCode == HttpStatusCode.NoContent;
    }

    public async Task<AuthentikPaginationResponse<AuthentikGroupResponse>?> GetGroups(string groupName)
    {
        var response = await GetAsync($"core/groups/?name={HttpUtility.UrlEncode(groupName)}");
        if (response.StatusCode != HttpStatusCode.OK)
            return null;

        var stringContent = response.Content.ReadAsStringAsync().Result;
        var data = JsonSerializer.Deserialize<AuthentikPaginationResponse<AuthentikGroupResponse>?>(
            stringContent, Program.SerializerOptions);
        return data;
    }
}

public class AuthentikGroupUserModifyRequest
{
    [JsonPropertyName("pk")]
    public int UserId { get; set; }
}
public class AuthentikGroupResponse
{
    [JsonPropertyName("pk")]
    public string Uuid { get; set; }
    [JsonPropertyName("num_pk")]
    public int UuidInt { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("is_superuser")]
    public bool IsSuperuser { get; set; }
    [JsonPropertyName("parent")]
    public string? ParentGroupUuid { get; set; }
    [JsonPropertyName("parent_name")]
    public string ParentGroupName { get; set; }
    [JsonPropertyName("users")]
    public int[] UserCount { get; set; }
    [JsonPropertyName("attributes")]
    public Dictionary<string, object> Attributes { get; set; }
    [JsonPropertyName("users_obj")]
    public AuthentikMinimalUser[] Users { get; set; }
}