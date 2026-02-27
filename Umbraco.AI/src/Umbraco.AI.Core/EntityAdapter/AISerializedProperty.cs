namespace Umbraco.AI.Core.EntityAdapter;

/// <summary>
/// Represents a serialized property for LLM context.
/// </summary>
/// <remarks>
/// This class is obsolete and kept for reference only.
/// Entity data is now stored in the <see cref="AISerializedEntity.Data"/> field as free-form JSON.
/// For CMS entities, properties are nested inside the data field as:
/// <code>
/// {
///   "contentType": "blogPost",
///   "properties": [
///     { "alias": "title", "label": "Title", "editorAlias": "Umbraco.TextBox", "value": "Hello" }
///   ]
/// }
/// </code>
/// </remarks>
[Obsolete("Entity data is now stored in AISerializedEntity.Data as free-form JSON. This class is kept for reference only.")]
public sealed class AISerializedProperty
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
