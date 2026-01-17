namespace Umbraco.Ai.Core.EntityAdapter;

/// <summary>
/// Represents a serialized property for LLM context.
/// </summary>
public sealed class AiSerializedProperty
{
    /// <summary>
    /// The property alias.
    /// </summary>
    public required string Alias { get; init; }

    /// <summary>
    /// The display label for the property.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// The property editor alias (e.g., "Umbraco.TextBox", "Umbraco.TextArea").
    /// </summary>
    public required string EditorAlias { get; init; }

    /// <summary>
    /// The current value of the property.
    /// </summary>
    public object? Value { get; init; }
}
