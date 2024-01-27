namespace XeniaBot.Shared;

public class MongoDBConfigItem
{
    public string ConnectionUrl { get; set; }
    public string DatabaseName { get; set; }

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