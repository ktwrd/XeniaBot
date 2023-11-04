using Microsoft.AspNetCore.Mvc;
using XeniaBot.Shared.Models;

namespace XeniaBot.WebPanel.Controllers;

public class HealthController : BaseXeniaController
{
    public HealthController()
        : base()
    {
    }

    [HttpGet("/Health")]
    [ProducesDefaultResponseType(type: typeof(XeniaHealthModel))]
    public IActionResult Health()
    {
        var data = new XeniaHealthModel()
        {
            StartTimestamp = Program.StartTimestamp,
            Version = Program.Version?.ToString() ?? "unknown (null)",
            ServiceName = "XeniaDiscordBot_Dashboard"
        };
        return Json(data, Program.SerializerOptions);
    }
}