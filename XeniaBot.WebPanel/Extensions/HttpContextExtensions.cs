using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using XeniaBot.Shared.Helpers;
using XeniaBot.WebPanel.Helpers;

namespace XeniaBot.WebPanel.Extensions;

public static class HttpContextExtensions
{
    public static async Task<AuthenticationScheme[]> GetExternalProvidersAsync(this HttpContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var schemes = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();

        return (from scheme in await schemes.GetAllSchemesAsync()
            where !string.IsNullOrEmpty(scheme.DisplayName)
            select scheme).ToArray();
    }

    public static async Task<bool> IsProviderSupportedAsync(this HttpContext context, string provider)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return (from scheme in await context.GetExternalProvidersAsync()
            where string.Equals(scheme.Name, provider, StringComparison.OrdinalIgnoreCase)
            select scheme).Any();
    }

    public static bool IsLoggedIn(this HttpContext context)
        => context.User?.Identity?.IsAuthenticated == true;

#pragma warning disable S6966
    public static async Task<IUser?> GetCurrentDiscordUser(this HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated != true) return null;
        var userId = AspHelper.GetUserId(context);
        if (!userId.HasValue) return null;

        var discord = context.RequestServices.GetRequiredService<DiscordSocketClient>();
        var user = await ExceptionHelper.RetryOnTimedOut(async () => discord.GetUser(userId.Value));
        return user;
    }

    public static async Task<IGuildUser?> GetCurrentDiscordGuildMember(this HttpContext context, ulong guildId)
    {
        var user = await context.GetCurrentDiscordUser();
        if (user == null) return null;

        var discord = context.RequestServices.GetRequiredService<DiscordSocketClient>();
        var guild = await ExceptionHelper.RetryOnTimedOut(async () => discord.GetGuild(guildId));
        if (guild == null) return null;

        var member = await ExceptionHelper.RetryOnTimedOut(async () => guild.GetUser(user.Id));
        return member;
    }
#pragma warning restore S6966
}