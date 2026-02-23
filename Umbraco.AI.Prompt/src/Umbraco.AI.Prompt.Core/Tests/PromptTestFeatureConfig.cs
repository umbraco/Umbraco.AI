using System.Text.Json;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Prompt.Core.Tests;

/// <summary>
/// Configuration for prompt test feature.
/// Defines mock entity context and prompt-specific settings for test execution.
/// </summary>
public class PromptTestFeatureConfig
{
    /// <summary>
    /// Mock entity context configuration (entity type, sub-type, and mock entity data).
    /// </summary>
    [AIField(
        Label = "Entity Context",
        Description = "Mock entity data to test with",
        EditorUiAlias = "Uai.PropertyEditorUi.TestEntityContext",
        SortOrder = 1)]
    public JsonElement? EntityContext { get; set; }

    /// <summary>
    /// The property alias being edited.
    /// </summary>
    [AIField(
        Label = "Property Alias",
        Description = "The property being edited by the prompt",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 2)]
    public string PropertyAlias { get; set; } = string.Empty;

    /// <summary>
    /// Optional culture variant.
    /// </summary>
    [AIField(
        Label = "Culture",
        Description = "Optional culture variant (e.g., en-US)",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 3)]
    public string? Culture { get; set; }

    /// <summary>
    /// Optional segment variant.
    /// </summary>
    [AIField(
        Label = "Segment",
        Description = "Optional segment variant",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 4)]
    public string? Segment { get; set; }

    /// <summary>
    /// AIContext entity IDs to include (overrides prompt's configured contexts).
    /// </summary>
    [AIField(
        Label = "Context IDs",
        Description = "AIContext entity IDs to include (overrides prompt's configured contexts)",
        EditorUiAlias = "Uai.PropertyEditorUi.ContextPicker",
        SortOrder = 7)]
    public List<Guid>? ContextIds { get; set; }
}
