using System;
using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Helpers;

public static class AuthAttributeHelper
{
    private static CoreContext Core => CoreContext.Instance!;
    /// <summary>
    /// <para>Handle setting the View to NotAuthorized and showing the login button.</para>
    ///
    /// <para>This will only be shown when the user isn't logged in</para> 
    /// </summary>
    /// <returns>When `false` is returned, the Result has been set to the NotAuthorized view.</returns>
    public static bool HandleAuth(ActionExecutingContext context)
    {
        var isAuth = context.HttpContext.User?.Identity?.IsAuthenticated ?? false;
        if (!isAuth)
        {
            context.Result = new ViewResult
            {
                ViewName = "NotAuthorized",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), context.ModelState)
                {
                    Model = new NotAuthorizedViewModel()
                    {
                        ShowLoginButton = true,
                        Message = "Please Login"
                    }
                }
            };
            return false;
        }

        return true;
    }

    /// <summary>
    /// <para>Handle setting the View to NotAuthorized when the provided <paramref name="userId"/> doesn't have the permission <paramref name="permissionRequired"/> in the provided <paramref name="guildId"/>.</para>
    ///
    /// <para>The <see cref="ActionExecutingContext.Result"/> will only be set when the provided user doesn't have the provided permission in the provided guild.</para>
    /// </summary>
    /// <param name="context">Context to use</param>
    /// <param name="userId">User Id to check</param>
    /// <param name="guildId">Guild Id to check</param>
    /// <param name="permissionRequired">
    /// <para>Permission that the User requires.</para>
    ///
    /// <para>Default: <see cref="GuildPermission.ManageGuild"/></para></param>
    /// <returns>`false` when <see cref="ActionExecutingContext.Result"/> has been modified.</returns>
    public static bool HandleUserAccessGuild(
        ActionExecutingContext context,
        ulong userId,
        ulong guildId,
        GuildPermission permissionRequired = GuildPermission.ManageGuild)
    {
        bool canAccess = AspHelper.CanAccessGuild(guildId, userId, permissionRequired);
        if (!canAccess)
        {
            context.Result = new ViewResult
            {
                ViewName = "NotAuthorized",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), context.ModelState)
                {
                    Model = new NotAuthorizedViewModel()
                    {
                        Message = "Missing permission Manage Server"
                    }
                }
            };
            return false;
        }

        return true;
    }
    /// <summary>
    /// <inheritdoc cref="HandleUserAccessGuild(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext,ulong,ulong,Discord.GuildPermission)"/>
    ///
    /// <para>Fetches the userId to use from <see cref="AspHelper.GetUserId"/></para>
    /// </summary>
    public static bool HandleUserAccessGuild(
        ActionExecutingContext context,
        ulong guildId,
        GuildPermission permissionRequired = GuildPermission.ManageGuild)
    {
        var userId = AspHelper.GetUserId(context.HttpContext) ?? 0;
        return HandleUserAccessGuild(context, userId, guildId, permissionRequired);
    }

    /// <summary>
    /// Parse and Fetch the Guild Id that is in use from the Route Data
    /// </summary>
    /// <param name="context">Context to use</param>
    /// <param name="routeDataKey">Key of the item in <see cref="ActionExecutingContext.RouteData"/> where the Guild Id is provided.</param>
    /// <returns>Parsed Guild Id</returns>
    /// <exception cref="Exception">Thrown when <paramref name="routeDataKey"/> is null</exception>
    public static ulong? ParseGuildIdFromRouteData(
        ActionExecutingContext context,
        string? routeDataKey)
    {
        
        if (routeDataKey == null)
        {
            Log.Error("GuildIdRouteKey has not been set!");
            throw new Exception("GuildIdRouteKey has not been set");
        }
        
        ulong? targetGuildId = null;
        if (context.RouteData.Values.TryGetValue(routeDataKey, out var s))
        {
            try
            {
                targetGuildId = ulong.Parse(s?.ToString() ?? "0");
                if (targetGuildId == null || targetGuildId < 1)
                    throw new Exception("Target Guild must not be null and greater than zero");
            }
            catch
            {
                targetGuildId = null;
            }
        }
        else
        {
            Log.Warn($"Route Key \"{routeDataKey}\" not found");
        }

        return targetGuildId;
    }
}