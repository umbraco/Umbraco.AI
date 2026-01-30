using Umbraco.Ai.Core.EditableModels;

namespace Umbraco.Ai.Agent.Core.Tests;

/// <summary>
/// Test case configuration for agent testing.
/// Defines the inputs needed to execute an agent test.
/// </summary>
public class AgentTestTestCase
{
    /// <summary>
    /// Conversation history as JSON array.
    /// </summary>
    [AiField(
        Label = "Messages",
        Description = "Conversation history as JSON array",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = "[{ \"alias\": \"rows\", \"value\": 10 }]",
        SortOrder = 1)]
    public string MessagesJson { get; set; } = "[]";

    /// <summary>
    /// Tool definitions as JSON array.
    /// </summary>
    [AiField(
        Label = "Tools",
        Description = "Tool definitions as JSON array (optional)",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = "[{ \"alias\": \"rows\", \"value\": 5 }]",
        SortOrder = 2)]
    public string? ToolsJson { get; set; }

    /// <summary>
    /// Initial state as JSON object.
    /// </summary>
    [AiField(
        Label = "State",
        Description = "Initial state as JSON object (optional)",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = "[{ \"alias\": \"rows\", \"value\": 3 }]",
        SortOrder = 3)]
    public string? StateJson { get; set; }

    /// <summary>
    /// Additional context variables.
    /// </summary>
    [AiField(
        Label = "Context Items",
        Description = "Additional context variables (JSON array)",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = "[{ \"alias\": \"rows\", \"value\": 5 }]",
        SortOrder = 4)]
    public string? ContextItemsJson { get; set; }
}
