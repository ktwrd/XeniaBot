using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace XeniaBot.WebPanel.Extensions;

public static class HtmxExtensions
{
    [NonAction]
    public static bool IsHtmxRequest(this Controller controller)
    {
        return controller.Request.Headers.Keys.Any(key => string.Equals(key, "hx-request", StringComparison.OrdinalIgnoreCase));
    }
    [NonAction]
    public static bool IsHtmxRequest(this HttpContext context)
    {
        return context.Request.Headers.Keys.Any(key => string.Equals(key, "hx-request", StringComparison.OrdinalIgnoreCase));
    }
}