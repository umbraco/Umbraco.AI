namespace Umbraco.Ai.Agent.Core.Agents;

/// <summary>
/// Defines where a prompt is allowed to run, both for UI display and server-side enforcement.
/// </summary>
public class AiAgentScope
{
    /// <summary>
    /// Rules that define where the prompt is allowed to run.
    /// If any rule matches, the prompt can execute (OR logic between rules).
    /// Empty means the prompt is not allowed anywhere.
    /// </summary>
    public IReadOnlyList<AiAgentScopeRule> AllowRules { get; set; } = [];

    /// <summary>
    /// Rules that define where the prompt is explicitly denied.
    /// If any rule matches, the prompt cannot execute (OR logic between rules).
    /// Deny rules take precedence over allow rules.
    /// </summary>
    public IReadOnlyList<AiAgentScopeRule> DenyRules { get; set; } = [];
}
