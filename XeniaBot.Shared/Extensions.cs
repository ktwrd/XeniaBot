using Discord;
using System;
using System.IO;
using System.Text;

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

    public static MemoryStream ToMemoryStream(this string value, Encoding encoding)
    {
        return new MemoryStream(encoding.GetBytes(value));
    }
}
