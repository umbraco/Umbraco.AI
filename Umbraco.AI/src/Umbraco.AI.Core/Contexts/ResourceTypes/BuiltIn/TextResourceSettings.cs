using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Contexts.ResourceTypes.BuiltIn;

/// <summary>
/// Settings model for the Text resource type.
/// </summary>
public sealed class TextResourceSettings
{
    /// <summary>
    /// The text content (can be plain text or markdown).
    /// </summary>
    [AIField(EditorUiAlias = "Umb.PropertyEditorUi.TextArea", SortOrder = 10)]
    public string? Content { get; set; }
}
