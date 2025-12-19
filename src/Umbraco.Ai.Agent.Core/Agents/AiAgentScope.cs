namespace Umbraco.Ai.Agent.Core.Agents;

/// <summary>
/// Defines where a agent is allowed to run, both for UI display and server-side enforcement.
/// </summary>
public class AiAgentScope
{
    /// <summary>
    /// Rules that define where the agent is allowed to run.
    /// If any rule matches, the agent can execute (OR logic between rules).
    /// Empty means the agent is not allowed anywhere.
    /// </summary>
    public IReadOnlyList<AiAgentScopeRule> AllowRules { get; set; } = [];

    /// <summary>
    /// Rules that define where the agent is explicitly denied.
    /// If any rule matches, the agent cannot execute (OR logic between rules).
    /// Deny rules take precedence over allow rules.
    /// </summary>
    public IReadOnlyList<AiAgentScopeRule> DenyRules { get; set; } = [];
}
