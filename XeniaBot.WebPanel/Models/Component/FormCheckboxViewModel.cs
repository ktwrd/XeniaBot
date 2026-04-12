using System;

namespace XeniaBot.WebPanel.Models;

public class FormCheckboxViewModel
{
    public string? ParentFormId { get; set; }
    public string Name { get; set; }
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Label { get; set; }
    public bool State { get; set; }
    public string? ExtraClasses { get; set; }
    public string CalculatedClasses
    {
        get
        {
            var s = "form-check";
            if (Margin)
            {
                s += " mb-3";
            }
            if (!string.IsNullOrEmpty(ExtraClasses?.Trim()))
            {
                s += " " + ExtraClasses;
            }
            return s;
        }
    }
    public bool Margin { get; set; } = false;
}