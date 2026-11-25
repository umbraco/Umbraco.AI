namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Thrown when an agent exists but the caller may not use it, because the agent is limited to user
/// groups the caller is not in. Callers map it to "Agent not available" (403 in the Management API).
/// </summary>
/// <remarks>
/// Derives from <see cref="InvalidOperationException"/>, like the other run refusals (agent not found,
/// agent not active), so existing handlers for those keep treating it as a failed run.
/// </remarks>
public sealed class AIAgentNotAvailableException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentNotAvailableException"/> class.
    /// </summary>
    /// <param name="agentId">The ID of the agent the caller may not use.</param>
    public AIAgentNotAvailableException(Guid agentId)
        : base($"Agent '{agentId}' is not available.")
    {
        AgentId = agentId;
    }

    /// <summary>
    /// Gets the ID of the agent the caller may not use.
    /// </summary>
    public Guid AgentId { get; }
}
