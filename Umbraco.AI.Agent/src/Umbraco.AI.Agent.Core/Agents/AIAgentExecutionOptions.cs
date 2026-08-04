using Umbraco.AI.Core.Chat;
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

    /// <summary>
    /// Controls how destructive backend tools are gated for human approval during this execution.
    /// Defaults to <see cref="AIApprovalPolicy.DenyAll"/> — the safe choice for non-interactive
    /// callers (programmatic execution, Automate), which have no way to resolve a
    /// <c>human_approval</c> interrupt. The AG-UI streaming path overrides this to
    /// <see cref="AIApprovalPolicy.Interactive"/> because it can pause and resume.
    /// </summary>
    public AIApprovalPolicy ApprovalPolicy { get; init; } = AIApprovalPolicy.DenyAll;

    /// <summary>
    /// Optional output schema to override the agent's configured <see cref="AIStandardAgentConfig.OutputSchema"/>.
    /// When set, the agent's response will be constrained to this schema.
    /// </summary>
    public AIOutputSchema? OutputSchema { get; init; }

    /// <summary>
    /// Optional additional properties to inject into the agent's runtime context for the duration
    /// of this execution. Keys placed here flow into <see cref="Umbraco.AI.Core.RuntimeContext.AIRuntimeContext"/>
    /// via <see cref="Chat.ScopedAIAgent"/> and are visible to chat middleware (including the audit-log
    /// middleware) at LLM-invocation time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// To have a key persisted onto the resulting <c>AIAuditLog.Metadata</c> column, also include
    /// <see cref="Umbraco.AI.Core.Constants.ContextKeys.LogKeys"/> in this dictionary with a
    /// <c>string[]</c> value listing the keys to persist. This mirrors the contract used by the
    /// AG-UI streaming path on <see cref="IAIAgentService"/>.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<string, object?>? AdditionalProperties { get; init; }

    /// <summary>
    /// Optional binding to a server-side conversation history store. When set, the agent is created
    /// with the supplied MAF chat-history provider attached and the run is bound to the conversation,
    /// so history is loaded/persisted through custom storage instead of the LLM service. Null (all
    /// current callers) leaves execution unchanged.
    /// </summary>
    public AIConversationHistoryBinding? ConversationHistory { get; init; }
}
