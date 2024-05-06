using System.Text.Json.Serialization;

namespace XeniaBot.Evidence.Responses;

public class FileControllerErrorResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; }

    public FileControllerErrorResponse()
    {
        Message = "";
    }
}