namespace XeniaBot.WebPanel.Models;

public class EmbedViewerModel
{
    public string Id { get; set; }
    public bool ShowImportButton { get; set; }
    public bool ShowExportButton { get; set; }

    public EmbedViewerModel(string id)
    {
        Id = id;
        ShowImportButton = false;
        ShowExportButton = false;
    }
}