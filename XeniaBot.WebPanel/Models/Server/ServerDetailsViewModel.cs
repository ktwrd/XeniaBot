using System.Collections.Generic;
using Discord.WebSocket;
using XeniaBot.MongoData.Models;
using XeniaBot.MongoData.Services;
using XeniaBot.WebPanel.Models.Component.FunView;
using XeniaDiscord.Data.Models.BanSync;

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
    public ServerLogModel LogConfig { get; set; }
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