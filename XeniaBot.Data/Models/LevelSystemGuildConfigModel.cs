using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models;

public class LevelSystemGuildConfigModel : BaseModel
{
    public ulong GuildId { get; set; }
    public ulong? LevelUpChannel { get; set; }
    public bool ShowLeveUpMessage { get; set; }
    public bool Enable { get; set; }

    public LevelSystemGuildConfigModel()
    {
        LevelUpChannel = null;
        Enable = true;
        ShowLeveUpMessage = true;
    }
}