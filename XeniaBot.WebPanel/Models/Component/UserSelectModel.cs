namespace XeniaBot.WebPanel.Models;

public class UserSelectModel : BaseFormItemModel
{
    public IEnumerable<StrippedUser> Users { get; set; }
    public ulong? SelectedUserId { get; set; }
    public string DisplayName { get; set; }
    public bool Required { get; set; }
}