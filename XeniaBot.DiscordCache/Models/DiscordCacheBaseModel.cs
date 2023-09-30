using XeniaBot.Shared.Models;

namespace XeniaBot.DiscordCache.Models;

public class DiscordCacheBaseModel : BaseModel
{
    public ulong Snowflake;
    public long ModifiedAtTimestamp;

    public DiscordCacheBaseModel()
    {
        ModifiedAtTimestamp = 0;
    }
}