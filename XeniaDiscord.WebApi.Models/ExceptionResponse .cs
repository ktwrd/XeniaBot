using System.Text.Json.Serialization;

namespace XeniaDiscord.WebApi.Models;

public class ExceptionResponse : BaseResponse
{
    public ExceptionResponse()
    {
        Success = false;
    }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("stackTrace")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StackTrace { get; set; }

    [JsonPropertyName("raw")]
    public string? Raw { get; set; }

    [JsonPropertyName("exceptionType")]
    public string? ExceptionType { get; set; }

    public void FromException(Exception ex)
    {
        Message = ex.Message;
        StackTrace = ex.StackTrace;
        Raw = ex.ToString();
        ExceptionType = ex.GetType().ToString();
    }
}
