using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace XeniaBot.Shared;

public class ConfigDataV1
{
    public static string[] IgnoredValidationKeys => new string[]
    {
        "DeveloperMode",
        "DeveloperMode_Server",
        "UserWhitelistEnable",
        "UserWhitelist",
        "Prefix",
        "GeneratorId",
        "OAuth_ClientId",
        "OAuth_ClientSecret",
        "Invite_ClientId",
        "DiscordBotList_Token",
        "AuthentikToken",
        "AuthentikUrl",
        "AuthentikEnable",
        "HasDashboard",
        "Health_Enable",
        "Health_Port",
        "BackpackTFApiKey"
    };
    
    public static string[] RequiredKeys => new string[]
    {
        "DiscordToken",
        "ErrorGuild",
        "ErrorChannel",
        "MongoDBServer",
        "Invite_ClientId",
        "Invite_Permissions",
        "UserWhitelist"
    };
    public static string[] RequiredBotKeys => RequiredKeys.Concat(new string[]
    {
        "HasDashboard",
        "DashboardLocation"
    }).Distinct().ToArray();
    public static string[] RequiredDashKeys => RequiredKeys.Concat(new string[]
    {
        "OAuth_ClientId",
        "OAuth_ClientSecret"
    }).Distinct().ToArray();
    
    /// <summary>
    /// Current config version.
    /// </summary>
    public uint Version
    {
        get => 1;
        set { value = 1; }
    }
    
    /// <summary>
    /// Discord user token
    /// </summary>
    public string DiscordToken = "";
    /// <summary>
    /// Restrict commands
    /// </summary>
    public bool DeveloperMode = true;
    /// <summary>
    /// ServerId to restrict commands to when <see cref="DeveloperMode"/> is `true`
    /// </summary>
    public ulong DeveloperMode_Server = 0;
    public bool UserWhitelistEnable = false;
    /// <summary>
    /// Text Command User whitelist when <see cref="UserWhitelistEnable"/> is `true`
    /// </summary>
    public ulong[] UserWhitelist = Array.Empty<ulong>();
    
    /// <summary>
    /// Text command prefix
    /// </summary>
    public string Prefix = "x.";

    /// <summary>
    /// Server Id for error logs
    /// </summary>
    public ulong ErrorGuild = 0;
    /// <summary>
    /// Channel Id in <see cref="ErrorGuild"/> for error logs
    /// </summary>
    public ulong ErrorChannel = 0;
    
    /// <summary>
    /// MongoDB Connection URI
    /// </summary>
    public string MongoDBServer = "";
    
    /// <summary>
    /// Used for custom snowflakes
    /// </summary>
    public int GeneratorId = 0;
    
    /// <summary>
    /// Server Id for Ban Sync stuff
    /// </summary>
    public ulong BanSync_AdminServer = 0;
    /// <summary>
    /// Channel Id for ban sync logs
    /// </summary>
    public ulong BanSync_GlobalLogChannel = 0;
    /// <summary>
    /// Channel Id for Ban Sync Requesting
    /// </summary>
    public ulong BanSync_RequestChannel = 0;
    
    /// <summary>
    /// API Key for weatherapi.com
    /// </summary>
    public string WeatherAPI_Key = "";
    
    /// <summary>
    /// Google Cloud Key for translation
    /// </summary>
    public GoogleCloudKey GCSKey_Translate = new GoogleCloudKey();
    
    /// <summary>
    /// Username for e621.net
    /// </summary>
    public string ESix_Username = "";
    /// <summary>
    /// Api key for e621.net
    /// </summary>
    public string ESix_ApiKey = "";

    /// <summary>
    /// Enable prometheus exporter
    /// </summary>
    public bool   Prometheus_Enable = true;
    /// <summary>
    /// Port to listen the prometheus exporter on
    /// </summary>
    public int    Prometheus_Port = 4828;
    /// <summary>
    /// Url for the prometheus exporter
    /// </summary>
    public string Prometheus_Url = "/metrics";
    /// <summary>
    /// Hostname for the prometheus exporter. `+` for all/any.
    /// </summary>
    public string Prometheus_Hostname = "+";

    /// <summary>
    /// ClientId for invite
    /// </summary>
    public ulong Invite_ClientId = 0;
    /// <summary>
    /// Calculated permissions for invite link
    /// </summary>
    public ulong Invite_Permissions = 415471496311;

    /// <summary>
    /// API Token for Authentik server
    /// </summary>
    public string AuthentikToken = "";
    /// <summary>
    /// Base URL for Authentik server
    /// </summary>
    public string AuthentikUrl = "";
    /// <summary>
    /// Enable Authentik admin module
    /// </summary>
    public bool   AuthentikEnable = false;

    /// <summary>
    /// Is there a dashboard setup for the bot
    /// </summary>
    public bool HasDashboard = false;
    /// <summary>
    /// Url for this bot's dashboard.
    /// </summary>
    public string DashboardLocation = "";
    
    /// <summary>
    /// Client ID for OAuth Web Panel
    /// </summary>
    public string OAuth_ClientId = "";
    /// <summary>
    /// Client Secret for OAuth Web Panel
    /// </summary>
    public string OAuth_ClientSecret = "";

    /// <summary>
    /// API Token for discordbotlist.com
    /// </summary>
    public string DiscordBotList_Token = "";

    public bool Health_Enable = false;
    public int Health_Port = 4829;

    /// <summary>
    /// API Key for Backpack.tf
    /// </summary>
    public string? BackpackTFApiKey = null;

    public bool Lavalink_Enable = false;
    public string? Lavalink_Hostname = null;
    public ushort Lavalink_Port = 2333;
    public string Lavalink_Auth = "";
    public bool Lavalink_Secure = false;

    public string SupportServerUrl = "";
}