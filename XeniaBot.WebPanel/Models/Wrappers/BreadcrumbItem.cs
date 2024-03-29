using System.Collections.Generic;

namespace XeniaBot.WebPanel.Models;

public class BreadcrumbItem
{
    public string? TargetController { get; set; }
    public string? TargetAction { get; set; }
    public Dictionary<string, string>? TargetRoute { get; set; }
    public string? Href { get; set; }
    public string Name { get; set; }

    public BreadcrumbItem(string name)
    {
        Name = name;
    }
    public BreadcrumbItem(string name, string href)
    {
        Name = name;
        Href = href;
    }
    public BreadcrumbItem(string name, string controller, string action, Dictionary<string, string>? route = null)
    {
        Name = name;
        TargetController = controller;
        TargetAction = action;
        TargetRoute = route ?? new Dictionary<string, string>();
    }
}