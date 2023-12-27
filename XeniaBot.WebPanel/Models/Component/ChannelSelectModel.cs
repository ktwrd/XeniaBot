namespace XeniaBot.WebPanel.Models;

public class ChannelSelectModel : BaseFormItemModel
{
    /// <summary>
    /// Channels to display.
    ///
    /// Generate with <see cref="StrippedChannel.FromGuild"/>
    /// </summary>
    public IEnumerable<StrippedChannel> Channels { get; set; }
    /// <summary>
    /// Current selected channel. `null` for the placeholder option to be selected
    /// </summary>
    public ulong? SelectedChannelId { get; set; }
    /// <summary>
    /// Content for `input-group` label. 
    /// </summary>
    public string DisplayName { get; set; }
    /// <summary>
    /// Categories to group channels into.
    /// </summary>
    public IEnumerable<StrippedCategory> Categories { get; set; }
}