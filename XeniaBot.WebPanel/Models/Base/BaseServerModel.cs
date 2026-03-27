using Discord.WebSocket;
using System.Collections.Generic;
using XeniaBot.MongoData.Models;
using XeniaDiscord.Data.Models.BanSync;

namespace XeniaBot.WebPanel.Models;

public interface IBaseServerModel
{
    public SocketGuild Guild { get; set; }
    public ICollection<SocketGuildUser> UsersWhoCanAccess { get; set; }
    public CounterGuildModel CounterConfig { get; set; }
    public BanSyncGuildModel BanSyncConfig { get; set; }
    public LevelSystemConfigModel XpConfig { get; set; }
    public ServerLogModel LogConfig { get; set; }
    public ICollection<BanSyncGuildSnapshotModel> BanSyncStateHistory { get; set; }
    public GuildGreeterConfigModel GreeterConfig { get; set; }
    public GuildByeGreeterConfigModel GreeterGoodbyeConfig { get; set; }
    public ICollection<GuildWarnItemModel> WarnItems { get; set; }
    public RolePreserveGuildModel RolePreserve { get; set; }
    public long BanSyncRecordCount { get; set; }
    public GuildConfigWarnStrikeModel WarnStrikeConfig { get; set; }
    public ConfessionGuildModel ConfessionConfig { get; set; }
}