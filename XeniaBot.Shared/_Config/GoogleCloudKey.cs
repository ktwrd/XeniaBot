using System.Text.Json.Serialization;

namespace XeniaBot.Shared;


public class GoogleCloudKey
{
    [JsonPropertyName("type")]
    public string Type;
    [JsonPropertyName("project_id")]
    public string ProjectId;
    [JsonPropertyName("private_key_id")]
    public string PrivateKeyId;
    [JsonPropertyName("private_key")]
    public string PrivateKey;
    [JsonPropertyName("client_email")]
    public string ClientEmail;
    [JsonPropertyName("client_id")]
    public string ClientId;
    [JsonPropertyName("auth_uri")]
    public string AuthUri;
    [JsonPropertyName("token_uri")]
    public string TokenUri;
    [JsonPropertyName("auth_provider_x509_cert_url")]
    public string AuthProviderCertUrl;
    [JsonPropertyName("client_x509_cert_url")]
    public string ClientCertUrl;
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