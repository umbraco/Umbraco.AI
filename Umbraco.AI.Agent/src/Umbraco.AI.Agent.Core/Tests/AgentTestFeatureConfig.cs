using System.Text.Json;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Tests;

namespace Umbraco.AI.Agent.Core.Tests;

/// <summary>
/// Configuration for agent test feature.
/// Defines entity context, messages, tools, state, and context for agent testing.
/// </summary>
public class AgentTestFeatureConfig : AITestFeatureConfigBase
{
    /// <summary>
    /// Messages to send to the agent.
    /// </summary>
    [AIField(
        Label = "Messages",
        Description = "Initial messages to send to the agent (JSON array)",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = "[{\"alias\":\"rows\",\"value\":5}]")]
    public List<AGUIMessage> Messages { get; set; } = [];

    /// <summary>
    /// Thread ID for the test run.
    /// </summary>
    [AIField(
        Label = "Thread ID",
        Description = "Optional thread ID (defaults to test ID)",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox")]
    public string? ThreadId { get; set; }
}
