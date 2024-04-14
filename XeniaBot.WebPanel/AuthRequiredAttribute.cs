using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;

namespace XeniaBot.WebPanel;

/// <summary>
/// Restrict usage of the Route for only authenticated users.
///
/// <para>Can be configured to only allow specified Guild (<see cref="GuildIdRouteDataName"/>) or if the user must be a bot owner (<see cref="RequireWhitelist"/>)</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class AuthRequiredAttribute : ActionFilterAttribute
{
    #region Obsolete
    /// <summary>
    /// <para><b>Will be removed in a later update</b></para>
    /// <para>Optional: Requesting User Id must be in the provided guild and must have the Manage Server permission.</para>
    /// </summary>
    [DefaultValue(null)]
    [Obsolete("Will be removed in a later update.")]
    public ulong? RequireGuildId { get; set; }
    /// <summary>
    /// <para><b>Will be removed in a later update. Use <see cref="RestrictToGuildAttribute"/> instead.</b></para>
    /// 
    /// <para>Route/Parameter name where the Guild Id is set.</para>
    ///
    /// <para>Requesting User must have the Manage Server permission</para>
    ///
    /// <para>Note: This overrides <see cref="RequireGuildId"/></para>
    /// </summary>
    [DefaultValue(null)]
    [Obsolete("Will be removed in a later update. Use RestrictToGuildAttribute instead.")]
    public string? GuildIdRouteDataName { get; set; }
    /// <summary>
    /// <para><b>Will be removed in a later update. Use <see cref="RequireSuperuserAttribute"/> instead.</b></para>
    /// Require the requesting user to be in <see cref="ConfigData.UserWhitelist"/>. Used for bot owner-only sections.
    /// </summary>
    [DefaultValue(false)]
    [Obsolete("Will be removed in a later update. Use RequireSuperuserAttribute instead.")]
    public bool RequireWhitelist { get; set; }
    #endregion
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Only allow authenticated users.
        if (!AuthAttributeHelper.HandleAuth(context))
            return;

        #region Obsolete
        if (RequireWhitelist)
        {
            var userId = AspHelper.GetUserId(context.HttpContext) ?? 0;
            if (!CoreContext.Instance?.GetRequiredService<ConfigData>().UserWhitelist.Contains((ulong)userId) ?? false)
            {
                context.Result = new ViewResult
                {
                    ViewName = "NotAuthorized",
                };
                return;
            }
            base.OnActionExecuting(context);
            return;
        }

        ulong? targetGuildId = RequireGuildId;
        if (GuildIdRouteDataName != null)
        {
            if (context.RouteData.Values.TryGetValue(GuildIdRouteDataName, out var s))
            {
                try
                {
                    targetGuildId = ulong.Parse(s?.ToString() ?? "0");
                    if (targetGuildId == null || targetGuildId < 1)
                        throw new Exception();
                }
                catch
                {
                    targetGuildId = null;
                }
            }

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
        }
        if (targetGuildId != null)
        {
            var userId = AspHelper.GetUserId(context.HttpContext) ?? 0;
            bool canAccess = AspHelper.CanAccessGuild((ulong)targetGuildId!, userId);
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
                return;
            }
        }
        #endregion
        
        base.OnActionExecuting(context);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthRequiredAttribute"/> class.
    /// </summary>
    public AuthRequiredAttribute()
    {
        RequireWhitelist = false;
    }
}