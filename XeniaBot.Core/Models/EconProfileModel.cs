namespace XeniaBot.Core.Models;

public class EconProfileModel : BaseModel
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public long Coins { get; set; }
    public long LastDailyTimestamp { get; set; }
}