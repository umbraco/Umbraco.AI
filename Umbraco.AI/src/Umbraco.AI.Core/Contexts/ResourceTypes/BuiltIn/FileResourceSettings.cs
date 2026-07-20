using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Contexts.ResourceTypes.BuiltIn;

/// <summary>
/// Settings model for the File (project-knowledge) resource type.
/// </summary>
public sealed class FileResourceSettings
{
    /// <summary>
    /// The display name of the uploaded file (e.g. "brand-guidelines.pdf").
    /// </summary>
    [AIField(EditorUiAlias = "Umb.PropertyEditorUi.TextBox", SortOrder = 10)]
    public string? FileName { get; set; }

    /// <summary>
    /// The extracted plain-text content of the file, injected as durable project knowledge.
    /// </summary>
    /// <remarks>
    /// MVP stores already-extracted text. TODO: a file-upload editor that extracts text at upload time
    /// (reusing the Agent file-processing pipeline) so editors upload a document directly.
    /// </remarks>
    [AIField(EditorUiAlias = "Umb.PropertyEditorUi.TextArea", EditorConfig = "[{ \"alias\": \"rows\", \"value\": 12 }]", SortOrder = 20)]
    public string? ExtractedText { get; set; }
}
