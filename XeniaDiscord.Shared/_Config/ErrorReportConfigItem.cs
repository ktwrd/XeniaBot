namespace XeniaBot.Shared;

public class ErrorReportConfigItem
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }

    public static ErrorReportConfigItem Default(ErrorReportConfigItem? i = null)
    {
        i ??= new ErrorReportConfigItem();
        i.GuildId = default;
        i.ChannelId = default;
        return i;
    }
}