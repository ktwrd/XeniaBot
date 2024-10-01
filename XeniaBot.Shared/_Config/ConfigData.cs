using System;
using System.ComponentModel;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using XeniaBot.Shared.Services;

namespace XeniaBot.Shared;

public class ConfigData
{
    /// <summary>
    /// Current config version.
    ///
    /// 2: New schema version. Requires total transformation.
    /// 3: Move MongoDB connection URL from MongoDBConnectionUrl to MongoDB.ConnectionUrl
    /// 4: Add property ReminderService. No upgrade required.
    /// 5: Add property IsUpgradeAgent. No upgrade required.
    /// 6: Add property Developer.GenericLoggingChannelId. No upgrade required.
    /// </summary>
    public uint Version
    {
        get => 6;
        set { value = 6; }
    }

    /// <summary>
    /// Discord token to login as.
    /// </summary>
    public string DiscordToken { get; set; }
    public string Prefix { get; set; }
    public ulong InviteClientId { get; set; }
    public ulong InvitePermissions { get; set; }
    /// <summary>
    /// Client ID for OAuth Web Panel
    /// </summary>
    public string OAuthId { get; set; }
    /// <summary>
    /// Client Secret for OAuth Web Panel
    /// </summary>
    public string OAuthSecret { get; set; }
    
    public DevModeConfigItem Developer { get; set; }
    public UserWhitelistConfigItem UserWhitelist { get; set; }
    public ErrorReportConfigItem ErrorReporting { get; set; }
    public BanSyncConfigItem BanSync { get; set; }
    public ApiKeyConfigItem ApiKeys { get; set; }
    public PrometheusConfigItem Prometheus { get; set; }
    public AuthentikConfigItem? Authentik { get; set; }
    public HealthConfigItem Health { get; set; }
    public LavalinkConfigItem Lavalink { get; set; }
    public GoogleCloudKey? GoogleCloud { get; set; }
    public MongoDBConfigItem MongoDB { get; set; }
    public ReminderServiceConfigItem ReminderService { get; set; }
    
    public string? SupportServerUrl { get; set; }
    public bool HasDashboard { get; set; }
    public string? DashboardUrl { get; set; }
    /// <summary>
    /// <para>Is this instance responsible for upgrading database schema?</para>
    ///
    /// <para>Should be enabled on Dashboard when deployed</para>
    /// </summary>
    public bool IsUpgradeAgent { get; set; }
    /// <summary>
    /// When set to <see langword="true"/>, this client will refresh bans on startup. Will not apply when not running as a bot.
    /// </summary>
    public bool RefreshBansOnStart { get; set; }
    [DefaultValue(null)]
    public int? ShardId { get; set; }

    public ConfigData()
    {
        Default(this);
    }
    
    public static ConfigData Default(ConfigData? i = null)
    {
        i ??= new ConfigData();
        i.DiscordToken = "";
        i.InviteClientId = default;
        i.InvitePermissions = default;
        i.Prefix = "x.";

        i.Developer = DevModeConfigItem.Default();
        i.UserWhitelist = UserWhitelistConfigItem.Default();
        i.ErrorReporting = ErrorReportConfigItem.Default();
        i.BanSync = BanSyncConfigItem.Default();
        i.ApiKeys = ApiKeyConfigItem.Default();
        i.Prometheus = PrometheusConfigItem.Default();
        i.Authentik = AuthentikConfigItem.Default();
        i.Health = HealthConfigItem.Default();
        i.GoogleCloud = null;
        i.MongoDB = MongoDBConfigItem.Default();
        i.ReminderService = ReminderServiceConfigItem.Default();
        i.IsUpgradeAgent = false;
        i.RefreshBansOnStart = true;
        i.ShardId = null;

        i.SupportServerUrl = null;
        i.HasDashboard = false;
        i.DashboardUrl = null;
        return i;
    }
    
    public static ConfigData Migrate(string content)
    {
        var b = JsonSerializer.Deserialize<ConfigDataVersionField>(content, ConfigService.SerializerOptions);
        var instance = new ConfigData();
        var version = JObject.Parse(content)["Version"]?.ToString();
        var jobject = JObject.Parse(content);
        switch (version)
        {
            case null:
                Log.Error($"Unable to determine version in config since it is null. Aborting.\nIf you recently upgraded and your config is relatively flat, try setting it to `1`.");
                Environment.Exit(1);
                break;
            case "1":
                var v1 = JsonSerializer.Deserialize<ConfigDataV1>(content, ConfigService.SerializerOptions);
                instance.DiscordToken = v1.DiscordToken;
                if (jobject.TryGetValue("MongoDBConnectionUrl", out var s))
                {
                    instance.MongoDB.ConnectionUrl = s.ToString();
                }
                instance.Prefix = v1.Prefix;
                
                instance.InviteClientId = v1.Invite_ClientId;
                instance.InvitePermissions = v1.Invite_Permissions;
                instance.OAuthId = v1.OAuth_ClientId;
                instance.OAuthSecret = v1.OAuth_ClientSecret;

                instance.Developer = new DevModeConfigItem()
                {
                    Enable = v1.DeveloperMode,
                    GuildId = v1.DeveloperMode_Server
                };
                instance.UserWhitelist = new UserWhitelistConfigItem()
                {
                    Enable = v1.UserWhitelistEnable,
                    Users = v1.UserWhitelist
                };
                instance.ErrorReporting = new ErrorReportConfigItem()
                {
                    GuildId = v1.ErrorGuild,
                    ChannelId = v1.ErrorChannel
                };
                instance.BanSync = new BanSyncConfigItem()
                {
                    GuildId = v1.BanSync_AdminServer,
                    LogChannelId = v1.BanSync_GlobalLogChannel,
                    RequestChannelId = v1.BanSync_RequestChannel
                };
                instance.ApiKeys = new ApiKeyConfigItem();
                instance.ApiKeys.Weather = v1.WeatherAPI_Key;
                instance.ApiKeys.BackpackTF = v1.BackpackTFApiKey;
                instance.ApiKeys.DiscordBotList = v1.DiscordBotList_Token;
                instance.ApiKeys.ESix = new ESixConfigItem()
                {
                    Username = v1.ESix_Username,
                    ApiKey = v1.ESix_ApiKey
                };

                instance.Prometheus = new PrometheusConfigItem()
                {
                    Enable = v1.Prometheus_Enable,
                    Hostname = v1.Prometheus_Hostname,
                    Port = v1.Prometheus_Port,
                    Url = v1.Prometheus_Url
                };
                instance.Health = new HealthConfigItem()
                {
                    Enable = v1.Health_Enable,
                    Port = v1.Health_Port
                };
                instance.Authentik = new AuthentikConfigItem()
                {
                    Enable = v1.AuthentikEnable,
                    Token = v1.AuthentikToken,
                    Url = v1.AuthentikUrl
                };
                instance.Lavalink = new LavalinkConfigItem()
                {
                    Auth = v1.Lavalink_Auth,
                    Enable = v1.Lavalink_Enable,
                    Hostname = v1.Lavalink_Hostname,
                    Port = v1.Lavalink_Port,
                    Secure = v1.Lavalink_Secure
                };
                instance.GoogleCloud = v1.GCSKey_Translate;

                instance.SupportServerUrl = v1.SupportServerUrl;
                instance.HasDashboard = v1.HasDashboard;
                instance.DashboardUrl = v1.DashboardLocation;
                return instance;
                break;
            case "2":
                instance = JsonSerializer.Deserialize<ConfigData>(content, ConfigService.SerializerOptions) ??
                           new ConfigData();
                if (jobject.TryGetValue("MongoDBConnectionUrl", out var sk))
                {
                    instance.MongoDB.ConnectionUrl = sk.ToString();
                }

                return instance;
                break;
        }
        return JsonSerializer.Deserialize<ConfigData>(content, ConfigService.SerializerOptions) ?? new ConfigData();
    }

    public static void Validate(ProgramDetails details, ConfigData i)
    {
        // TODO Implement Validation1
    }
}