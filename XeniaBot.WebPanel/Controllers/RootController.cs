using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Controllers;

[Route("/")]
[Controller]
public class RootController : Controller
{
    [HttpGet("/403")]
    public IActionResult NotAuth(NotAuthorizedViewModel model)
    {
        return View("NotAuthorized", model);
    }
}