using System;
using System.Linq;

namespace XeniaBot.Shared;

public class ConfigDataV1
{
    public static string[] IgnoredValidationKeys =>
    [
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
    ];
    
    public static string[] RequiredKeys =>
    [
        "DiscordToken",
        "ErrorGuild",
        "ErrorChannel",
        "MongoDBServer",
        "Invite_ClientId",
        "Invite_Permissions",
        "UserWhitelist"
    ];
    public static string[] RequiredBotKeys => RequiredKeys.Concat(new[]
    {
        "HasDashboard",
        "DashboardLocation"
    }).Distinct().ToArray();
    public static string[] RequiredDashKeys => RequiredKeys.Concat(new[]
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
    public string DiscordToken { get; set; } = "";
    /// <summary>
    /// Restrict commands
    /// </summary>
    public bool DeveloperMode { get; set; } = true;
    /// <summary>
    /// ServerId to restrict commands to when <see cref="DeveloperMode"/> is `true`
    /// </summary>
    public ulong DeveloperMode_Server { get; set; } = 0;
    public bool UserWhitelistEnable { get; set; } = false;
    /// <summary>
    /// Text Command User whitelist when <see cref="UserWhitelistEnable"/> is `true`
    /// </summary>
    public ulong[] UserWhitelist { get; set; } = Array.Empty<ulong>();
    
    /// <summary>
    /// Text command prefix
    /// </summary>
    public string Prefix { get; set; } = "x.";

    /// <summary>
    /// Server Id for error logs
    /// </summary>
    public ulong ErrorGuild { get; set; } = 0;
    /// <summary>
    /// Channel Id in <see cref="ErrorGuild"/> for error logs
    /// </summary>
    public ulong ErrorChannel { get; set; } = 0;
    
    /// <summary>
    /// MongoDB Connection URI
    /// </summary>
    public string MongoDBServer { get; set; } = "";
    
    /// <summary>
    /// Used for custom snowflakes
    /// </summary>
    public int GeneratorId { get; set; } = 0;
    
    /// <summary>
    /// Server Id for Ban Sync stuff
    /// </summary>
    public ulong BanSync_AdminServer { get; set; } = 0;
    /// <summary>
    /// Channel Id for ban sync logs
    /// </summary>
    public ulong BanSync_GlobalLogChannel { get; set; } = 0;
    /// <summary>
    /// Channel Id for Ban Sync Requesting
    /// </summary>
    public ulong BanSync_RequestChannel { get; set; } = 0;
    
    /// <summary>
    /// API Key for weatherapi.com
    /// </summary>
    public string WeatherAPI_Key { get; set; } = "";
    
    /// <summary>
    /// Google Cloud Key for translation
    /// </summary>
    public GoogleCloudKey GCSKey_Translate { get; set; } = new GoogleCloudKey();
    
    /// <summary>
    /// Username for e621.net
    /// </summary>
    public string ESix_Username { get; set; } = "";
    /// <summary>
    /// Api key for e621.net
    /// </summary>
    public string ESix_ApiKey { get; set; } = "";

    /// <summary>
    /// Enable prometheus exporter
    /// </summary>
    public bool Prometheus_Enable { get; set; } = true;
    /// <summary>
    /// Port to listen the prometheus exporter on
    /// </summary>
    public int Prometheus_Port { get; set; } = 4828;
    /// <summary>
    /// Url for the prometheus exporter
    /// </summary>
    public string Prometheus_Url { get; set; } = "/metrics";
    /// <summary>
    /// Hostname for the prometheus exporter. `+` for all/any.
    /// </summary>
    public string Prometheus_Hostname { get; set; } = "+";

    /// <summary>
    /// ClientId for invite
    /// </summary>
    public ulong Invite_ClientId { get; set; } = 0;
    /// <summary>
    /// Calculated permissions for invite link
    /// </summary>
    public ulong Invite_Permissions { get; set; } = 415471496311;

    /// <summary>
    /// API Token for Authentik server
    /// </summary>
    public string AuthentikToken { get; set; } = "";
    /// <summary>
    /// Base URL for Authentik server
    /// </summary>
    public string AuthentikUrl { get; set; } = "";
    /// <summary>
    /// Enable Authentik admin module
    /// </summary>
    public bool AuthentikEnable { get; set; } = false;

    /// <summary>
    /// Is there a dashboard setup for the bot
    /// </summary>
    public bool HasDashboard { get; set; } = false;
    /// <summary>
    /// Url for this bot's dashboard.
    /// </summary>
    public string DashboardLocation { get; set; } = "";
    
    /// <summary>
    /// Client ID for OAuth Web Panel
    /// </summary>
    public string OAuth_ClientId { get; set; } = "";
    /// <summary>
    /// Client Secret for OAuth Web Panel
    /// </summary>
    public string OAuth_ClientSecret { get; set; } = "";

    /// <summary>
    /// API Token for discordbotlist.com
    /// </summary>
    public string DiscordBotList_Token { get; set; } = "";

    public bool Health_Enable { get; set; } = false;
    public int Health_Port { get; set; } = 4829;

    /// <summary>
    /// API Key for Backpack.tf
    /// </summary>
    public string? BackpackTFApiKey { get; set; } = null;

    public bool Lavalink_Enable { get; set; } = false;
    public string? Lavalink_Hostname { get; set; } = null;
    public ushort Lavalink_Port { get; set; } = 2333;
    public string Lavalink_Auth { get; set; } = "";
    public bool Lavalink_Secure { get; set; } = false;

    public string SupportServerUrl { get; set; } = string.Empty;
}