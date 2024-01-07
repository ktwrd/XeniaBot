namespace XeniaBot.Shared;

public class ApiKeyConfigItem
{
    public string? Weather { get; set; }
    public ESixConfigItem ESix { get; set; }
    public string? DiscordBotList { get; set; }
    public string? BackpackTF { get; set; }

    public static ApiKeyConfigItem Default(ApiKeyConfigItem? i = null)
    {
        i ??= new ApiKeyConfigItem();
        i.Weather = null;
        i.ESix = ESixConfigItem.Default();
        i.DiscordBotList = null;
        i.BackpackTF = null;
        return i;
    }

    public ApiKeyConfigItem()
    {
        Default(this);
    }
}