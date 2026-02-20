using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Prompt.Core.Tests;

/// <summary>
/// Configuration for prompt test feature.
/// Defines what content/media to test with and whether to use real or mock data.
/// </summary>
public class PromptTestFeatureConfig
{
    /// <summary>
    /// The type of entity (document, media, member).
    /// </summary>
    [AIField(
        Label = "Entity Type",
        Description = "Type of entity being tested",
        EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
        EditorConfig = "[{\"alias\":\"multiple\",\"value\":false},{\"alias\":\"items\",\"value\":[\"document\",\"media\",\"member\"]}]",
        SortOrder = 1)]
    public string EntityType { get; set; } = "document";

    /// <summary>
    /// The entity ID to test with (content/media item).
    /// </summary>
    [AIField(
        Label = "Entity ID",
        Description = "The content or media item to test with",
        EditorUiAlias = "Uai.PropertyEditorUi.EntityPicker",
        EditorConfig = "[{\"alias\":\"entityTypeField\",\"value\":\"entityType\"}]",
        SortOrder = 2)]
    public Guid? EntityId { get; set; }

    /// <summary>
    /// The property alias being edited.
    /// </summary>
    [AIField(
        Label = "Property Alias",
        Description = "The property being edited by the prompt",
        EditorUiAlias = "Uai.PropertyEditorUi.EntityPropertyPicker",
        EditorConfig = "[{\"alias\":\"entityTypeField\",\"value\":\"entityType\"},{\"alias\":\"entityIdField\",\"value\":\"entityId\"}]",
        SortOrder = 3)]
    public string PropertyAlias { get; set; } = string.Empty;

    /// <summary>
    /// Optional culture variant.
    /// </summary>
    [AIField(
        Label = "Culture",
        Description = "Optional culture variant (e.g., en-US)",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 4)]
    public string? Culture { get; set; }

    /// <summary>
    /// Optional segment variant.
    /// </summary>
    [AIField(
        Label = "Segment",
        Description = "Optional segment variant",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 5)]
    public string? Segment { get; set; }

    /// <summary>
    /// Additional context items to provide to the prompt.
    /// </summary>
    [AIField(
        Label = "Context Items",
        Description = "Additional context variables (JSON array)",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = "[{\"alias\":\"rows\",\"value\":3}]",
        SortOrder = 7)]
    public List<AIRequestContextItem>? ContextItems { get; set; }
}
