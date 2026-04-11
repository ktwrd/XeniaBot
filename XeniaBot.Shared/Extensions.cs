using Discord;

namespace XeniaBot.Shared;

public static class Extensions
{
    public static string FormatUsername(this IUser user)
    {
        if (user.DiscriminatorValue == 0) return user.Username;
        return $"{user.Username}#{user.Discriminator}";
    }
}
