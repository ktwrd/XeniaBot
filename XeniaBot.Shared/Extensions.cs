using Discord;
using System;

namespace XeniaBot.Shared;

public static class Extensions
{
    public static string FormatUsername(this IUser user)
    {
        return user.DiscriminatorValue == 0
            ? user.Username
            : $"{user.Username}#{user.Discriminator}";
    }

    public static bool IsMissingDiscordPermissions(this Exception ex)
    {
        var str = ex.ToString();
        return str.Contains("Missing Access", StringComparison.OrdinalIgnoreCase)
            || str.Contains("50001", StringComparison.OrdinalIgnoreCase)
            || str.Contains("50013", StringComparison.OrdinalIgnoreCase);
    }
}
