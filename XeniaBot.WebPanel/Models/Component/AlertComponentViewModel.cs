namespace XeniaBot.WebPanel.Models;

public class AlertComponentViewModel : IAlertViewModel
{
    public string? Message { get; set; }
    public string? MessageType { get; set; }
    public string MessageClass => MessageType == null ? "alert " : $"alert alert-{MessageType}";
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