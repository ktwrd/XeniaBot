namespace XeniaBot.WebPanel.Models;

public class GuildBannerViewParameters
{
    public ulong GuildId { get; set; }
    public bool EnableBreadcrumb { get; set; }
    public List<BreadcrumbItem> Breadcrumbs { get; set; }

    public GuildBannerViewParameters(ulong id, bool breadcrumb = false, List<BreadcrumbItem>? items = null)
    {
        GuildId = id;
        EnableBreadcrumb = breadcrumb;
        Breadcrumbs = items ?? new List<BreadcrumbItem>();
    }
}