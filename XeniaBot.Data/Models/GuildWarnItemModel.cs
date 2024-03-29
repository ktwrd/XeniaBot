using System;
using kate.shared.Helpers;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models;

public class GuildWarnItemModel : BaseModel
{
    public static string CollectionName => "guildWarnItems";
    
    public string WarnId { get; set; }
    public ulong GuildId { get; set; }
    /// <summary>
    /// User Id for who this warn is for
    /// </summary>
    public ulong TargetUserId { get; set; }
    /// <summary>
    /// User Id for who warned <see cref="TargetUserId"/>
    /// </summary>
    public ulong ActionedUserId { get; set; }
    public long ModifiedAtTimestamp { get; set; }
    public long CreatedAtTimestamp { get; set; }
    public string Description { get; set; }

    public GuildWarnItemModel()
        : base()
    {
        WarnId = Guid.NewGuid().ToString();
    }
}