using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models;

public class UserConfigModel : BaseModel
{
    public ulong UserId { get; set; }
    public long ModifiedAtTimestamp { get; set; }
    
    public bool EnableProfileTracking { get; set; }
    public ListViewStyle ListViewStyle { get; set; }

    public void Defaults()
    {
        UserConfigModel.Defaults(this);
    }

    public static void Defaults(UserConfigModel model)
    {
        model.EnableProfileTracking = true;
        model.ListViewStyle = ListViewStyle.List;
    }
    
    public UserConfigModel()
    { Defaults(); }
}