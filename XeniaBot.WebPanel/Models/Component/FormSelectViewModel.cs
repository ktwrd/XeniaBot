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
    /// ```csharp
    /// new FormSelectViewModel()
    /// {
    ///     ParentFormId = "test_form",
    ///     Name = "test",
    ///     Id = "test_select",
    ///     Label = "Example",
    ///     Data = new List&lt;object, string>()
    ///     {
    ///         (1, "Hello")
    ///     },
    ///     Selected = null
    /// };
    /// ```
    /// would result in the following HTML being generated
    /// ```html
    /// &lt;label for="test_select">Example&lt;/label>
    /// &lt;select class="custom-select" form="test_form" name="test" id="test_select">
    ///     &lt;option value="1">Hello&lt;/option>
    /// &lt;/select>
    /// ```
    /// </example>
    public List<(object, string)> Data { get; set; }
    /// <summary>
    /// Value of the selected option element.
    /// </summary>
    public object? Selected { get; set; }
}