using System.Text.Json;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Agent.Core.Tests;

/// <summary>
/// Configuration for agent test feature.
/// Defines entity context, messages, tools, state, and context for agent testing.
/// </summary>
public class AgentTestFeatureConfig
{
    /// <summary>
    /// Mock entity context configuration (entity type, sub-type, and mock entity data).
    /// </summary>
    [AIField(
        Label = "Entity Context",
        Description = "Mock entity data to test with",
        EditorUiAlias = "Uai.PropertyEditorUi.TestEntityContext",
        SortOrder = 0)]
    public JsonElement? EntityContext { get; set; }

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
    /// AIContext entity IDs to include (overrides agent's configured contexts).
    /// </summary>
    [AIField(
        Label = "Context IDs",
        Description = "AIContext entity IDs to include (overrides agent's configured contexts)",
        EditorUiAlias = "Uai.PropertyEditorUi.ContextPicker",
        SortOrder = 5)]
    public List<Guid>? ContextIds { get; set; }

    /// <summary>
    /// Thread ID for the test run.
    /// </summary>
    [AIField(
        Label = "Thread ID",
        Description = "Optional thread ID (defaults to test ID)",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 6)]
    public string? ThreadId { get; set; }
}
