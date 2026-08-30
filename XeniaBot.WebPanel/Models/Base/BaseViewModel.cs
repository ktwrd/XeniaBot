using Discord.WebSocket;
using XeniaBot.MongoData.Models;

namespace XeniaBot.WebPanel.Models;

public class BaseViewModel : IBaseViewModel
{
    public DiscordShardedClient Client { get; set; }
    public UserConfigModel UserConfig { get; set; }
    
    public string? MessageType { get; set; }
    public string? Message { get; set; }
}

public interface IBaseViewModel : IAlertViewModel
{
    public DiscordShardedClient Client { get; set; }
    public UserConfigModel UserConfig { get; set; }
}