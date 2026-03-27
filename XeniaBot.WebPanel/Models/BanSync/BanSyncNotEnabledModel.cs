using Discord;

namespace XeniaBot.WebPanel.Models.BanSync;

public class BanSyncNotEnabledModel
{
    public required ulong GuildId { get; set; }
    public required IGuild? Guild { get; set; }
}
