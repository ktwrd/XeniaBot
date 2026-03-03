namespace XeniaBot.Shared;

public class PostgresConfigItem
{
    public string Host { get; set; } = "db";
    public int Port { get; set; } = 5432;
    public string Username { get; set; } = "xeniadiscord";
    public string Password { get; set; } = "password";
    public string DatabaseName { get; set; } = "xeniadiscordmainline";

    public static PostgresConfigItem Default(PostgresConfigItem? i = null)
    {
        i ??= new PostgresConfigItem();
        i.Host = "db";
        i.Port = 5432;
        i.Username = "xeniadiscord";
        i.Password = "password";
        i.DatabaseName = "xeniadiscordmainline";
        return i;
    }

    public PostgresConfigItem()
    {
        Default(this);
    }
}
