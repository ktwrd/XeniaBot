using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models;

public class UserConfigModel : BaseModel
{
    public ulong UserId { get; set; }
    public long ModifiedAtTimestamp { get; set; }
    
    public bool EnableProfileTracking { get; set; }
    public ListViewStyle ListViewStyle { get; set; }

    /// <summary>
    /// Reset this instance to default options. <see cref="Defaults(UserConfigModel)"/>
    /// </summary>
    public void Defaults()
    {
        UserConfigModel.Defaults(this);
    }

    /// <summary>
    /// Set default options on target model provided (<paramref name="model"/>)
    /// </summary>
    /// <param name="model">Target instance to reset default options to.</param>
    public static void Defaults(UserConfigModel model)
    {
        model.EnableProfileTracking = true;
        model.ListViewStyle = ListViewStyle.List;
    }
    
    public UserConfigModel()
    { Defaults(); }
}