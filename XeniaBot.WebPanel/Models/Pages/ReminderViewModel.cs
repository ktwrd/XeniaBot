using XeniaBot.Data.Models;

namespace XeniaBot.WebPanel.Models;

public class ReminderViewModel : BaseViewModel
{
    public ICollection<ReminderModel> Reminders { get; set; }
}