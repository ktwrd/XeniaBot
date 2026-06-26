using System.Collections.Generic;

namespace XeniaBot.WebPanel.Models;

public class FormSelectViewModel
{
    /// <summary>
    /// `form` attribute for the `select` element.
    /// </summary>
    public string ParentFormId { get; set; }
    /// <summary>
    /// `name` attribute for the `select` element.
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// `id` attribute for the `select` element, and the `for` attribute on the `label` element if it exists.
    /// </summary>
    public string Id { get; set; }
    /// <summary>
    /// When null, no label element will be generated.
    /// </summary>
    public string? Label { get; set; }
    /// <summary>
    /// List of select value and innerHTML.
    /// </summary>
    /// <example>
    /// With the following options
    /// <code>
    /// new FormSelectViewModel()
    /// {
    ///     ParentFormId = "test_form",
    ///     Name = "test",
    ///     Id = "test_select",
    ///     Label = "Example",
    ///     Data = new List&lt;object, string&gt;()
    ///     {
    ///         (1, "Hello")
    ///     },
    ///     Selected = null
    /// };
    /// </code>
    /// would result in the following HTML being generated
    /// <code>
    /// &lt;label for="test_select"&gt;Example&lt;/label&gt;
    /// &lt;select class="form-select" form="test_form" name="test" id="test_select"&gt;
    ///     &lt;option value="1"&gt;Hello&lt;/option&gt;
    /// &lt;/select&gt;
    /// </code>
    /// </example>
    public List<(object, string)> Data { get; set; } = [];
    /// <summary>
    /// Value of the selected option element.
    /// </summary>
    public object? Selected { get; set; }
}