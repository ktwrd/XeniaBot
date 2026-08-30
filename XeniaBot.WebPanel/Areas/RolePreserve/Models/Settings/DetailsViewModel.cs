using System.Collections.Generic;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Areas.RolePreserve.Models.Settings;

public class DetailsViewModel
{
    public ulong GuildId => Guild.Id;
    public required StrippedGuild Guild { get; set; }
    public bool Enable { get; set; }
    public List<StrippedRole> RoleBlacklist { get; set; } = [];
    public List<StrippedRole> AvailableRoles { get; set; } = [];
    // TODO implement this once proper logging has been implemented for role preserve
    // public StrippedChannel? LogChannel { get; set; }
    // public List<StrippedChannel> AvailableChannels { get; set; } = [];

    public AlertComponentViewModel? Alert { get; set; }
}