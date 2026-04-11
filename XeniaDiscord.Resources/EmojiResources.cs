using System.Reflection;
using static XeniaDiscord.Resources.ResourceHelper;

namespace XeniaDiscord.Resources;

public static class EmojiResources
{
    private const string BaseNamespace = "XeniaDiscord.Resources.Emojis";
    private static Assembly Assembly => typeof(EmojiResources).Assembly;
    public static MemoryStream GreenCheck => GetMemoryStream($"{BaseNamespace}.greencheck.png", Assembly);
    public static MemoryStream RedCross => GetMemoryStream($"{BaseNamespace}.redcross.png", Assembly);
    public static MemoryStream StatusDoNotDisturb => GetMemoryStream($"{BaseNamespace}.status-dnd.png", Assembly);
    public static MemoryStream StatusIdle => GetMemoryStream($"{BaseNamespace}.status-idle.png", Assembly);
    public static MemoryStream StatusOffline => GetMemoryStream($"{BaseNamespace}.status-offline.png", Assembly);
    public static MemoryStream StatusOnline => GetMemoryStream($"{BaseNamespace}.status-online.png", Assembly);
}