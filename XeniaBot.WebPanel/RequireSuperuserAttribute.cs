using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using XeniaBot.WebPanel.Helpers;

namespace XeniaBot.WebPanel;

/// <summary>
/// Restrict usage of the Route for only users that are in <see cref="ConfigData.UserWhitelist"/>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RequireSuperuserAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Only allow authenticated users.
        if (!AuthAttributeHelper.HandleAuth(context))
            return;

        // Decline access to users that aren't a superuser.
        var userId = AspHelper.GetUserId(context.HttpContext) ?? 0;
        if (CoreContext.Instance?.GetRequiredService<ConfigData>().UserWhitelist.Contains(userId) != true)
        {
            context.Result = new ViewResult
            {
                ViewName = "NotAuthorized",
            };
            return;
        }
        
        base.OnActionExecuting(context);
    }
}