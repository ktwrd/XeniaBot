using System.ComponentModel;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models;

public class UserConfigModel : BaseModel
{
    public static string CollectionName => "userConfig";
    public ulong UserId { get; set; }
    public long ModifiedAtTimestamp { get; set; }
    [DefaultValue(true)]
    public bool EnableProfileTracking { get; set; }
    [DefaultValue(false)]
    public bool SilentJoinMessage { get; set; }
    [DefaultValue(ListViewStyle.List)]
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
        model.SilentJoinMessage = false;
        model.ListViewStyle = ListViewStyle.List;
    }
    
    public UserConfigModel()
    { Defaults(); }
}