using System.ComponentModel;

namespace XeniaBot.Shared;

public class DevModeConfigItem
{
    public bool Enable { get; set; }
    /// <summary>
    /// GuildId to restrict things to. This doesn't really work all the time, it's best to have a separate private bot for development/testing.
    /// </summary>
    public ulong GuildId { get; set; }
    [DefaultValue(null)]
    public ulong? GenericLoggingChannelId { get; set; }

    public static DevModeConfigItem Default(DevModeConfigItem? i = null)
    {
        i ??= new DevModeConfigItem();
        i.Enable = false;
        i.GuildId = 0;
        i.GenericLoggingChannelId = null;
        return i;
    }

    public DevModeConfigItem()
    {
        Default(this);
    }
}