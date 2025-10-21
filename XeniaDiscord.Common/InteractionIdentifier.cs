namespace XeniaDiscord.Common;

public static class InteractionIdentifier
{
    public const string ConfessionModalCreate = "ns:event:discord:modalinteraction:confession:create";
    public const string ConfessionModal = "ns:event:discord:modalinteraction:confession:modal";
    public const string ConfessionModalContent = ConfessionModal + ":content";

    public const string WeatherTodayRefresh = "ns:event:discord:interaction:weather:today:refresh";
    public const string WeatherForecastRefresh = "ns:event:discord:interaction:weather:forecast:refresh";
}
