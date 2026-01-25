using System.ComponentModel;

namespace XeniaBot.Shared;

public class MongoDBConfigItem
{
    [DefaultValue("")]
    public string ConnectionUrl { get; set; } = "";
    [DefaultValue("xenia_discord")]
    public string DatabaseName { get; set; } = "xenia_discord";

    public static MongoDBConfigItem Default(MongoDBConfigItem? i = null)
    {
        i ??= new MongoDBConfigItem();
        i.ConnectionUrl = "";
        i.DatabaseName = "xenia_discord";
        return i;
    }

    public MongoDBConfigItem()
    {
        Default(this);
    }
}