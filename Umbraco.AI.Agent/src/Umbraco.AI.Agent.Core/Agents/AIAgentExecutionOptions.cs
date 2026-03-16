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

    /// <summary>
    /// Optional guardrail IDs to override for testing guardrail behavior.
    /// When set, this value is stored in the runtime context for guardrail resolvers to pick up.
    /// </summary>
    public IReadOnlyList<Guid>? GuardrailIdsOverride { get; init; }
}
