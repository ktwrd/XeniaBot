using System.Collections.Generic;
using System.Text.Json.Serialization;
using Discord.WebSocket;
using XeniaBot.MongoData.Models;
using XeniaBot.MongoData.Services;
using XeniaBot.WebPanel.Models.Component.FunView;
using XeniaDiscord.Data.Models.BanSync;
using XeniaDiscord.Data.Models.ServerLog;
using RolePreserveGuildModel = XeniaDiscord.Data.Models.RolePreserve.RolePreserveGuildModel;

namespace XeniaBot.WebPanel.Models;

public class ServerDetailsViewModel : BaseViewModel,
    IBaseServerModel,
    IServerLevelSystemComponentViewModel,
    IServerCountingComponentViewModel,
    IServerConfessionComponentViewModel
{
    public SocketGuildUser User { get; set; }
    public SocketGuild Guild { get; set; }
    public ICollection<SocketGuildUser> UsersWhoCanAccess { get; set; } = [];
    
    public CounterGuildModel CounterConfig { get; set; }
    public BanSyncGuildModel BanSyncConfig { get; set; }
    public LevelSystemConfigModel XpConfig { get; set; }
    public LevelSystemConfigModel LevelSystemConfig
    {
        get => XpConfig;
        set
        {
            XpConfig = value;
        }
    }
    public ServerLogGuildModel LogConfig { get; set; }
    public ICollection<BanSyncGuildSnapshotModel> BanSyncStateHistory { get; set; } = [];
    public GuildGreeterConfigModel GreeterConfig { get; set; }
    public GuildByeGreeterConfigModel GreeterGoodbyeConfig { get; set; }
    public ICollection<GuildWarnItemModel> WarnItems { get; set; } = [];
    public RolePreserveGuildModel RolePreserve { get; set; }
    public long BanSyncRecordCount { get; set; }
    public GuildConfigWarnStrikeModel WarnStrikeConfig { get; set; }
    public ConfessionGuildModel ConfessionConfig { get; set; }

    public bool IsWarnActive(GuildWarnItemModel model)
    {
        return WarnStrikeService.IsWarnActive(model, WarnStrikeConfig);
    }
}

public class JsTypeServerLogChannelItem
{
    [JsonPropertyName("category")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsCategory { get; set; }

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsText { get; set; }

    [JsonPropertyName("voice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsVoice { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = "0";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("children")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ICollection<JsTypeServerLogChannelItem>? Children { get; set; }
}
public class JsTypeServerLogConfigItem
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    
    [JsonPropertyName("channelId")]
    public required string ChannelId { get; set; }

    [JsonPropertyName("event")]
    public required string Event { get; set; }
}