using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Prompt.Core.Tests;

/// <summary>
/// Test case configuration for prompt testing.
/// Defines what content/media to test with and whether to use real or mock data.
/// </summary>
public class PromptTestTestCase
{
    /// <summary>
    /// The entity ID to test with (content/media item).
    /// </summary>
    [AIField(
        Label = "Entity ID",
        Description = "The content or media item to test with",
        PropertyEditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 1)]
    public Guid? EntityId { get; set; }

    /// <summary>
    /// The type of entity (document, media, member).
    /// </summary>
    [AIField(
        Label = "Entity Type",
        Description = "Type of entity being tested",
        PropertyEditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
        EditorConfig = "[{\"alias\":\"multiple\",\"value\":false},{\"alias\":\"items\",\"value\":[\"document\",\"media\",\"member\"]}]",
        SortOrder = 2)]
    public string EntityType { get; set; } = "document";

    /// <summary>
    /// The property alias being edited.
    /// </summary>
    [AIField(
        Label = "Property Alias",
        Description = "The property being edited by the prompt",
        PropertyEditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 3)]
    public string PropertyAlias { get; set; } = string.Empty;

    /// <summary>
    /// Optional culture variant.
    /// </summary>
    [AIField(
        Label = "Culture",
        Description = "Optional culture variant (e.g., en-US)",
        PropertyEditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 4)]
    public string? Culture { get; set; }

    /// <summary>
    /// Optional segment variant.
    /// </summary>
    [AIField(
        Label = "Segment",
        Description = "Optional segment variant",
        PropertyEditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 5)]
    public string? Segment { get; set; }

    /// <summary>
    /// Whether to use real entity data instead of mock data.
    /// </summary>
    [AIField(
        Label = "Use Real Entity",
        Description = "Use actual entity data instead of mock data",
        PropertyEditorUiAlias = "Umb.PropertyEditorUi.Toggle",
        SortOrder = 6)]
    public bool UseRealEntity { get; set; }

    /// <summary>
    /// Additional context items to provide to the prompt.
    /// </summary>
    [AIField(
        Label = "Context Items",
        Description = "Additional context variables (JSON array)",
        PropertyEditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = "[{\"alias\":\"rows\",\"value\":3}]",
        SortOrder = 7)]
    public List<AIRequestContextItem>? ContextItems { get; set; }
}
