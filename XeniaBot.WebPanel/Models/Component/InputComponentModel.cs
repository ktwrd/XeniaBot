namespace XeniaBot.WebPanel.Models;

public class InputComponentModel : BaseFormItemModel
{
    public string? Value { get; set; }
    public string Type { get; set; }
    public bool InputGroup { get; set; }
    public bool Required { get; set; }
    public string? DisplayName { get; set; }
    /// <summary>
    /// Text for input.
    /// </summary>
    public string? Placeholder { get; set; }
    /// <summary>
    /// Toggle the `readonly` attribute. When <see cref="Type"/> is `button`, or `checkbox`, it will toggle the `disabled` attribute instead.
    /// </summary>
    public bool Readonly { get; set; }
    /// <summary>
    /// Stuff to append to the `class` attribute on the input element.
    /// </summary>
    public string ExtraClass { get; set; }
    /// <summary>
    /// Will toggle the `checked` attribute when <see cref="Type"/> is `checkbox`.
    /// </summary>
    public bool Checked { get; set; }

    public InputComponentModel()
    {
        Type = "text";
        InputGroup = true;
        Readonly = false;
        ExtraClass = "";
        Checked = false;
        Required = false;
    }
}