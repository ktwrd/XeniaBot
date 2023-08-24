namespace XeniaBot.WebPanel.Models;

public class NotAuthorizedViewModel
{
    public string? Message { get; set; }
    public bool ShowLoginButton { get; set; }

    public NotAuthorizedViewModel()
    {
        ShowLoginButton = false;
    }
}