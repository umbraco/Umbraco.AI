namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Options for controlling agent execution behavior.
/// </summary>
public class AIAgentExecutionOptions
{
    /// <summary>
    /// Optional profile ID to override the agent's configured profile.
    /// Used for cross-model comparison testing.
    /// </summary>
    public Guid? ProfileIdOverride { get; init; }

    /// <summary>
    /// Optional context IDs to override the agent's configured <see cref="AIAgent.ContextIds"/>.
    /// Used for context comparison testing.
    /// </summary>
    public IReadOnlyList<Guid>? ContextIdsOverride { get; init; }
}
