using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class AuthRequiredAttribute : ActionFilterAttribute
{
    public bool ShowLoginButton { get; set; }
    public ulong? RequireGuildId { get; set; }
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var isAuth = context.HttpContext.User?.Identity?.IsAuthenticated ?? false;
        if (isAuth)
        {
            base.OnActionExecuting(context);
            return;
        }

        if (RequireGuildId != null)
        {
            var userId = AspHelper.GetUserId(context.HttpContext) ?? 0;
            ulong guildId = (ulong)RequireGuildId;
            bool canAccess = AspHelper.CanAccessGuild(guildId, userId);
            if (canAccess)
            {
                base.OnActionExecuting(context);
                return;
            }
        }
        var data = new NotAuthorizedViewModel
        {
            ShowLoginButton = ShowLoginButton
        };
        /*var view = new Views_Shared_NotAuthorized();
        view.ViewData.Model = data;
        var result = new ViewResult
        {
            ViewName = "NotAuthorized",
            ViewData = view.ViewData
        };*/
        var result = new RedirectToActionResult("NotAuth", "Root", data)
        {
            PreserveMethod = false,
        };
        context.Result = result;
    }

    public AuthRequiredAttribute()
    {
        ShowLoginButton = false;
    }
}