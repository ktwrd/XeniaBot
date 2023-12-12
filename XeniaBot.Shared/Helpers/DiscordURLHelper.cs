namespace XeniaBot.Shared.Helpers;

public static class DiscordURLHelper
{
    public const string UrlPrefix = "https://discord.com";

    public static string GuildChannelMessage(ulong guildId, ulong channelId, ulong messageId) =>
        $"{GuildChannel(guildId, channelId)}/{messageId}";

    public static string GuildChannel(ulong guildId, ulong channelId) =>
        $"{Guild(guildId)}/{channelId}";

    public static string Guild(ulong guildId) => $"{UrlPrefix}/channels/{guildId}";
}