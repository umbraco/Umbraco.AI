using System.Text.Json;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Agent.Core.Tests;

/// <summary>
/// Test case configuration for agent testing.
/// Defines what messages, tools, state, and context to test with.
/// </summary>
public class AgentTestTestCase
{
    /// <summary>
    /// Messages to send to the agent.
    /// </summary>
    [AIField(
        Label = "Messages",
        Description = "Initial messages to send to the agent (JSON array)",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = "[{\"alias\":\"rows\",\"value\":5}]",
        SortOrder = 1)]
    public List<AGUIMessage> Messages { get; set; } = [];

    /// <summary>
    /// Tools available to the agent.
    /// </summary>
    [AIField(
        Label = "Tools",
        Description = "Optional tools available to the agent (JSON array)",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = "[{\"alias\":\"rows\",\"value\":3}]",
        SortOrder = 2)]
    public List<AGUITool>? Tools { get; set; }

    /// <summary>
    /// Initial state for the agent.
    /// </summary>
    [AIField(
        Label = "Initial State",
        Description = "Optional initial state (JSON object)",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = "[{\"alias\":\"rows\",\"value\":3}]",
        SortOrder = 3)]
    public JsonElement? State { get; set; }

    /// <summary>
    /// Additional context items.
    /// </summary>
    [AIField(
        Label = "Context Items",
        Description = "Optional context items (JSON array)",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = "[{\"alias\":\"rows\",\"value\":3}]",
        SortOrder = 4)]
    public List<AGUIContextItem>? Context { get; set; }

    /// <summary>
    /// Thread ID for the test run.
    /// </summary>
    [AIField(
        Label = "Thread ID",
        Description = "Optional thread ID (defaults to test ID)",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 5)]
    public string? ThreadId { get; set; }
}
