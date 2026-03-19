using Umbraco.AI.Core.RuntimeContext;

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

    /// <summary>
    /// Optional context items for headless execution.
    /// Replaces AG-UI context conversion when executing agents programmatically.
    /// </summary>
    public IEnumerable<AIRequestContextItem>? ContextItems { get; init; }

    /// <summary>
    /// Optional user group IDs for permission resolution in headless contexts where
    /// no BackOffice user is available. When null, falls back to the current BackOffice user's groups.
    /// </summary>
    public IEnumerable<Guid>? UserGroupIds { get; init; }
}
