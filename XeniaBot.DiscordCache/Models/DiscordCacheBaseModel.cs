using XeniaBot.Shared.Models;

namespace XeniaBot.DiscordCache.Models;

public class DiscordCacheBaseModel : BaseModel
{
    public ulong Snowflake { get; set; }
    public long ModifiedAtTimestamp { get; set; }

    public DiscordCacheBaseModel()
    {
        ModifiedAtTimestamp = 0;
    }
}