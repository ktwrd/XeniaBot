using System.Text.Json.Serialization;
using XeniaDiscord.Shared;

namespace XeniaDiscord.WebApi.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(HealthResponse), Identifier.Event.Xenia.Health)]
[JsonDerivedType(typeof(ExceptionResponse), Identifier.Event.Xenia.Exception)]
public class BaseResponse
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("$success")]
    public bool? Success { get; set; }
}
