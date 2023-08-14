using Discord.WebSocket;
using Microsoft.AspNetCore.Components;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Components
{
    public partial class ChannelSelectComponent : ComponentBase
    {
        [Parameter]
        public IEnumerable<StrippedChannel> Channels { get; set; }
        [Parameter]
        public ulong? SelectedChannelId { get; set; }
        [Parameter]
        public string ParentFormId { get; set; }
        [Parameter]
        public string Name { get; set; }
        [Parameter]
        public string Id { get; set; }
        [Parameter]
        public string DisplayName { get; set; }
    }
}
