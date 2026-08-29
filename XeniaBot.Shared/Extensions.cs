using Discord;
using System;
using System.IO;
using System.Text;
using CSharpFunctionalExtensions;

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

    public static T? ToNullable<T>(this Maybe<T> value)
        where T : struct
    {
        return value.HasValue ? value.Value : null;
    }

    public static long ToUnixTimeSeconds(this DateTime value, TimeSpan offset)
    {
        return new DateTimeOffset(value, offset).ToUnixTimeSeconds();
    }
    public static long ToUnixTimeSeconds(this DateTime value) => value.ToUnixTimeSeconds(TimeSpan.Zero);
    public static long ToUnixTimeMilliseconds(this DateTime value, TimeSpan offset)
    {
        return new DateTimeOffset(value, offset).ToUnixTimeMilliseconds();
    }
    public static long ToUnixTimeMilliseconds(this DateTime value) => value.ToUnixTimeMilliseconds(TimeSpan.Zero);
}
