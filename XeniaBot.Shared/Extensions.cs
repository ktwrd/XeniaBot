using Discord;
using System;

namespace XeniaBot.Shared;

public static class Extensions
{
    public static string FormatUsername(this IUser user)
    {
        if (user.DiscriminatorValue == 0) return user.Username;
        return $"{user.Username}#{user.Discriminator}";
    }

    public static bool IsMissingDiscordPermissions(this Exception ex)
    {
        var str = ex.ToString();
        return str.Contains("Missing Access", StringComparison.OrdinalIgnoreCase)
            || str.Contains("50001")
            || str.Contains("50013");
    }
}
