using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Tests;

namespace Umbraco.AI.Agent.Core.Tests;

/// <summary>
/// Configuration for agent test feature.
/// Defines entity context, message, and context for agent testing.
/// </summary>
public class AgentTestFeatureConfig : AITestFeatureConfigBase
{
    /// <summary>
    /// The user message to send to the agent.
    /// </summary>
    [AIField(
        Label = "Message",
        Description = "The user message to send to the agent",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = "[{\"alias\":\"rows\",\"value\":3}]")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Thread ID for the test run.
    /// </summary>
    [AIField(
        Label = "Thread ID",
        Description = "Optional thread ID (defaults to test ID)",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox")]
    public string? ThreadId { get; set; }
}
