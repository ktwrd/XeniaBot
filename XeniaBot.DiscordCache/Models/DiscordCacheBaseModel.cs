using XeniaBot.Shared.Models;

namespace XeniaBot.DiscordCache.Models;

public class DiscordCacheBaseModel : BaseModel, IDiscordCacheBaseModel
{
    public ulong Snowflake { get; set; }
    public long ModifiedAtTimestamp { get; set; }

    public DiscordCacheBaseModel()
    {
        ModifiedAtTimestamp = 0;
    }
}

public interface IDiscordCacheBaseModel
{
    public ulong Snowflake { get; set; }
    public long ModifiedAtTimestamp { get; set; }
}