using Umbraco.Ai.Core.EditableModels;

namespace Umbraco.Ai.Prompt.Core.Tests;

/// <summary>
/// Test case configuration for prompt testing.
/// Defines the inputs needed to execute a prompt test.
/// </summary>
public class PromptTestTestCase
{
    /// <summary>
    /// The content/media item ID to test with.
    /// If null, a mock entity will be used.
    /// </summary>
    [AiField(
        Label = "Entity ID",
        Description = "The content/media item to test with (optional - uses mock if not provided)",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 1)]
    public Guid? EntityId { get; set; }

    /// <summary>
    /// Type of entity being edited.
    /// </summary>
    [AiField(
        Label = "Entity Type",
        Description = "Type of entity",
        EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
        EditorConfig = "[{ \"alias\": \"items\", \"value\": [\"document\", \"media\", \"member\"] }]",
        SortOrder = 2)]
    public string EntityType { get; set; } = "document";

    /// <summary>
    /// The property being edited (for context).
    /// </summary>
    [AiField(
        Label = "Property Alias",
        Description = "The property being edited",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 3)]
    public string PropertyAlias { get; set; } = string.Empty;

    /// <summary>
    /// Optional culture variant.
    /// </summary>
    [AiField(
        Label = "Culture",
        Description = "Optional culture variant",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 4)]
    public string? Culture { get; set; }

    /// <summary>
    /// Optional segment variant.
    /// </summary>
    [AiField(
        Label = "Segment",
        Description = "Optional segment variant",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 5)]
    public string? Segment { get; set; }

    /// <summary>
    /// Whether to use the actual entity data instead of a mock.
    /// </summary>
    [AiField(
        Label = "Use Real Entity",
        Description = "Use actual entity data instead of mock",
        EditorUiAlias = "Umb.PropertyEditorUi.Toggle",
        SortOrder = 6)]
    public bool UseRealEntity { get; set; }

    /// <summary>
    /// Additional context variables to provide to the prompt.
    /// </summary>
    [AiField(
        Label = "Context Items",
        Description = "Additional context variables (JSON array)",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = "[{ \"alias\": \"rows\", \"value\": 5 }]",
        SortOrder = 7)]
    public string? ContextItemsJson { get; set; }
}
