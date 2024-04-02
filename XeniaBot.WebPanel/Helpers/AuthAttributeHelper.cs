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
}