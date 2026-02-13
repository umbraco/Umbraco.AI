namespace Umbraco.AI.Prompt.Web.Api.Management.Prompt.Models;

/// <summary>
/// Represents a single result option that can be displayed and optionally applied.
/// </summary>
public class ResultOptionModel
{
    /// <summary>
    /// Short label/title for the option (e.g., "Formal Tone", "Result").
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// The display value to show in the UI.
    /// </summary>
    public required string DisplayValue { get; init; }

    /// <summary>
    /// Optional description explaining this option.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The property change to apply when this option is selected.
    /// Null for informational-only options.
    /// </summary>
    public PropertyChangeModel? PropertyChange { get; init; }
}
