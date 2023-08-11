using System;
using System.Text.Json.Serialization;

namespace XeniaBot.Shared;

public class ConfigData
{
    public static string[] IgnoredValidationKeys => new string[]
    {
        "DeveloperMode",
        "DeveloperMode_Server",
        "UserWhitelistEnable",
        "UserWhitelist",
        "Prefix",
        "GeneratorId"
    };
    
    public string DiscordToken = "";
    public bool DeveloperMode = true;
    public ulong DeveloperMode_Server = 0;
    public bool UserWhitelistEnable = false;
    public ulong[] UserWhitelist = Array.Empty<ulong>();
    public string Prefix = "x.";
    public ulong ErrorChannel = 0;
    public ulong ErrorGuild = 0;
    public string MongoDBServer = "";
    public int GeneratorId = 0;
    public ulong BanSync_AdminServer = 0;
    public ulong BanSync_GlobalLogChannel = 0;
    public ulong BanSync_RequestChannel = 0;
    public string WeatherAPI_Key = "";
    public GoogleCloudKey GCSKey_Translate = new GoogleCloudKey();
    public string ESix_Username = "";
    public string ESix_ApiKey = "";

    public bool   Prometheus_Enable = true;
    public int    Prometheus_Port = 4828;
    public string Prometheus_Url = "/metrics";
    public string Prometheus_Hostname = "+";

    public ulong Invite_ClientId = 0;
    public ulong Invite_Permissions = 415471496311;
}
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