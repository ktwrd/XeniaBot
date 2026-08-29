namespace XeniaBot.WebPanel.Models;

public class AlertComponentViewModel : IAlertViewModel
{
    public string? Message { get; set; }
    public bool? RenderMessageAsMarkdown { get; set; }
    public string? MessageType { get; set; }

    public AlertComponentType? Type
    {
        get;
        set
        {
            field = value;
            MessageType = value?.ToString().ToLower();
        }
    }
    public string MessageClass => MessageType == null ? "alert alert-primary alert-dismissible " : $"alert alert-dismissible alert-{MessageType}";
    public bool ShowClose { get; set; } = false;

    public static AlertComponentViewModel FromExisting(IAlertViewModel model, bool showClose = false)
    {
        return new AlertComponentViewModel()
        {
            Message = model.Message,
            MessageType = model.MessageType,
            ShowClose = true
        };
    }
}

public enum AlertComponentType
{
    Primary,
    Secondary,
    Success,
    Danger,
    Warning,
    Info
}