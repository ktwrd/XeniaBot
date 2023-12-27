namespace XeniaBot.WebPanel.Models;

public class BaseFormItemModel
{
    /// <summary>
    /// `form` attribute value on `select` element.
    /// </summary>
    public string ParentFormId { get; set; }
    /// <summary>
    /// `name` attribute value on `select` element.
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// `id` attribute value on `select` element.
    /// </summary>
    public string Id { get; set; }
}