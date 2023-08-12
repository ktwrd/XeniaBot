using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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