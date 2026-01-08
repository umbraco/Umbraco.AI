using Umbraco.Ai.Core.EditableModels;

namespace Umbraco.Ai.Core.Context.ResourceTypes.BuiltIn;

/// <summary>
/// Data model for the Text resource type.
/// </summary>
public sealed class TextResourceData
{
    /// <summary>
    /// The text content (can be plain text or markdown).
    /// </summary>
    [AiField(EditorUiAlias = "Umb.PropertyEditorUi.MarkdownEditor", SortOrder = 10)]
    public string? Content { get; set; }
}
