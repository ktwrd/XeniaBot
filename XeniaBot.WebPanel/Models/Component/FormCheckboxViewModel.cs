namespace XeniaBot.WebPanel.Models;

public class FormCheckboxViewModel
{
    public string ParentFormId { get; set; }
    public string Name { get; set; }
    public string Id { get; set; }
    public string Label { get; set; }
    public bool State { get; set; }
    public string? ExtraClasses { get; set; }
    public bool Margin { get; set; } = false;
}