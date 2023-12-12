namespace XeniaBot.Shared.Helpers;

public static class DiscordURLHelper
{
    public const string UrlPrefix = "https://discord.com";

    public static string GuildChannelMessage(ulong guildId, ulong channelId, ulong messageId) =>
        $"{UrlPrefix}/channels/{guildId}/{channelId}/{messageId}";
}