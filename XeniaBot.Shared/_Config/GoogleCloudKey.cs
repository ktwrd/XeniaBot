using System.Text.Json.Serialization;

namespace XeniaBot.Shared;

public class GoogleCloudKey
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("project_id")]
    public string ProjectId { get; set; }
    [JsonPropertyName("private_key_id")]
    public string PrivateKeyId { get; set; }
    [JsonPropertyName("private_key")]
    public string PrivateKey { get; set; }
    [JsonPropertyName("client_email")]
    public string ClientEmail { get; set; }
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; }
    [JsonPropertyName("auth_uri")]
    public string AuthUri { get; set; }
    [JsonPropertyName("token_uri")]
    public string TokenUri { get; set; }
    [JsonPropertyName("auth_provider_x509_cert_url")]
    public string AuthProviderCertUrl { get; set; }
    [JsonPropertyName("client_x509_cert_url")]
    public string ClientCertUrl { get; set; }
    public GoogleCloudKey()
    {
        Type = "";
        ProjectId = "";
        PrivateKeyId = "";
        PrivateKey = "";
        ClientEmail = "";
        ClientId = "";
        AuthUri = "";
        TokenUri = "";
        AuthProviderCertUrl = "";
        ClientCertUrl = "";
    }
}