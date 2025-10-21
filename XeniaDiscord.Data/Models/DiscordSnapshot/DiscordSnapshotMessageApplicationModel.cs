using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeniaDiscord.Data.Models.DiscordSnapshot;

public class DiscordSnapshotMessageApplicationModel
{
    public const string TableName = "DiscordSnapshotMessageApplication";

    public Guid SnapshotMessageId { get; set; }

    public string ApplicationId { get; set; } = "0"; // ulong as string
    public string? CoverImageId { get; set; }
    public string? Description { get; set; }
    public string? IconId { get; set; }
    public string? IconUrl { get; set; }
    public string Name { get; set; } = "";
}
