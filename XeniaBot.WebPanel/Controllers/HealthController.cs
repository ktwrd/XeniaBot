using Microsoft.AspNetCore.Mvc;

namespace XeniaBot.WebPanel.Controllers;

public class HealthController : BaseXeniaController
{
    public HealthController()
        : base()
    {
    }

    [HttpGet("/Health")]
    public IActionResult Health()
    {
        var data = new Dictionary<string, object>()
        {
            {"StartTimestamp", Program.StartTimestamp},
            {"Version", Program.Version},
            {"ServiceName", "XeniaDiscordBot"}
        };
        return Json(data, Program.SerializerOptions);
    }
}