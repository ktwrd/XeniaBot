using Discord.WebSocket;
using XeniaBot.Data.Models;

namespace XeniaBot.WebPanel.Models;

public class BaseViewModel : IBaseViewModel
{
    public DiscordSocketClient Client { get; set; }
    public UserConfigModel UserConfig { get; set; }
    
    public string? MessageType { get; set; }
    public string? Message { get; set; }
}

public interface IBaseViewModel : IAlertViewModel
{
    public DiscordSocketClient Client { get; set; }
    public UserConfigModel UserConfig { get; set; }
}