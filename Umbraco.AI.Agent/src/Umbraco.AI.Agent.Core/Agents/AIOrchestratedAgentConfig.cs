using System.Text.Json;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Configuration for an orchestrated agent that uses a registered workflow.
/// </summary>
public sealed class AIOrchestratedAgentConfig : IAIAgentConfig
{
    /// <summary>
    /// The ID of the registered workflow to use.
    /// </summary>
    public string? WorkflowId { get; set; }

    /// <summary>
    /// Workflow-specific settings as structured JSON.
    /// </summary>
    public JsonElement? Settings { get; set; }
}
