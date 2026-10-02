namespace Umbraco.AI.Automate.Actions;

/// <summary>
/// A single option for <see cref="AskChoiceDecisionSettings.Options"/>, edited through the
/// <c>Uai.PropertyEditorUi.KeyValueList</c> property editor. <see cref="Key"/> and <see cref="Value"/>
/// map property-for-property to the editor's row shape (<c>{ key, value }</c>), and round-trip
/// through <c>EditableModelResolver</c>'s camelCase JSON options without any explicit
/// <c>JsonPropertyName</c> attributes.
/// </summary>
public sealed class AskChoiceDecisionOption
{
    /// <summary>
    /// Gets or sets the option's key — the machine value returned when the AI picks this option.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional human-readable description of the option, shown to the AI to
    /// help it choose. A blank value maps to no description (<see langword="null"/>).
    /// </summary>
    public string? Value { get; set; }
}
