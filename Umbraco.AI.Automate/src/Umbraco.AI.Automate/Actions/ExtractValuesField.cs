namespace Umbraco.AI.Automate.Actions;

/// <summary>
/// One field in <see cref="ExtractValuesSettings.Fields"/>, edited through the
/// <c>Uai.PropertyEditorUi.ExtractionFieldList</c> property editor. Flat, not polymorphic
/// (same as <see cref="AskDecisionsQuestion"/>): properties irrelevant to <see cref="Kind"/> are
/// left unset. Stored as camelCase JSON:
/// <c>{ kind, alias, instructions, decimals, integer, unit, itemType }</c>.
/// </summary>
public sealed class ExtractValuesField
{
    /// <summary>
    /// Gets or sets the field kind: <c>"text"</c>, <c>"number"</c>, <c>"date"</c>, <c>"time"</c>,
    /// <c>"datetime"</c>, <c>"duration"</c> or <c>"list"</c>.
    /// </summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field's output key, also sent to Core as the field id. Core validates its
    /// shape and uniqueness.
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>Gets or sets what to extract: the natural-language instructions given to the model.</summary>
    public string Instructions { get; set; } = string.Empty;

    /// <summary>Gets or sets the decimal places to round to. Number and duration fields only.</summary>
    public int? Decimals { get; set; }

    /// <summary>Gets or sets whether the value must be a whole number. Number and duration fields only.</summary>
    public bool Integer { get; set; }

    /// <summary>
    /// Gets or sets the unit. For number fields, free text (e.g. <c>"DKK"</c>); for duration fields,
    /// one of the <c>AIDurationUnit</c> names (e.g. <c>"days"</c>, case-insensitive).
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Gets or sets the type of a list's items: <c>"text"</c> (default), <c>"number"</c> or
    /// <c>"date"</c>. List fields only.
    /// </summary>
    public string? ItemType { get; set; }
}
