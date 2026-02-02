using Umbraco.Cms.Core.PropertyEditors;

namespace Umbraco.AI.Core.PropertyEditors;

/// <summary>
/// Configuration for the AI Context Picker property editor.
/// </summary>
public class AIContextPickerConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether multiple contexts can be selected.
    /// </summary>
    [ConfigurationField("multiple")]
    public bool Multiple { get; set; }

    /// <summary>
    /// Gets or sets the minimum number of contexts required.
    /// </summary>
    [ConfigurationField("min")]
    public int? Min { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of contexts allowed.
    /// </summary>
    [ConfigurationField("max")]
    public int? Max { get; set; }
}
