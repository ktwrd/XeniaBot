using System;
using System.ComponentModel;
using Discord;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using XeniaBot.Shared;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel;

/// <summary>
/// Only restrict access to users who can access the guild provided.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RestrictToGuildAttribute : ActionFilterAttribute
{
    /// <summary>
    /// Key for the Route Data that contains the Guild Id this should be restricted to.
    /// </summary>
    [DefaultValue(null)]
    public string? GuildIdRouteKey { get; set; }
    /// <summary>
    /// Permission that the Requesting User requires to access the page this Attribute is applied on.
    /// </summary>
    [DefaultValue(GuildPermission.ManageGuild)]
    public GuildPermission RequiredPermission { get; set; }

    /// <inheritdoc />
    public RestrictToGuildAttribute()
        : base()
    {
        RequiredPermission = GuildPermission.ManageGuild;
    }
    
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Only allow authenticated users.
        if (!AuthAttributeHelper.HandleAuth(context))
            return;

        ulong? targetGuildId = AuthAttributeHelper.ParseGuildIdFromRouteData(context, GuildIdRouteKey);
        
        if (targetGuildId == null)
        {
            context.Result = new ViewResult
            {
                ViewName = "NotAuthorized",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), context.ModelState)
                {
                    Model = new NotAuthorizedViewModel()
                    {
                        Message = "Invalid Guild Provided"
                    }
                }
            };
            return;
        }


        if (!AuthAttributeHelper.HandleUserAccessGuild(context, (ulong)targetGuildId!))
        {
            return;
        }
        
        base.OnActionExecuting(context);
    }
}