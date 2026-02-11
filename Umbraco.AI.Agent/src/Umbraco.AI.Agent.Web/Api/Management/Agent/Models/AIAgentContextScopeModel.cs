namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Models;

/// <summary>
/// API model for agent context scope.
/// Defines where an agent is available using allow and deny rules.
/// </summary>
public class AIAgentContextScopeModel
{
    /// <summary>
    /// Rules that define where the agent is available.
    /// If any rule matches, the agent can be used (OR logic between rules).
    /// Empty means the agent is available everywhere (unless denied).
    /// </summary>
    public IReadOnlyList<AIAgentContextScopeRuleModel> AllowRules { get; set; } = [];

    /// <summary>
    /// Rules that define where the agent is explicitly denied.
    /// If any rule matches, the agent cannot be used (OR logic between rules).
    /// Deny rules take precedence over allow rules.
    /// </summary>
    public IReadOnlyList<AIAgentContextScopeRuleModel> DenyRules { get; set; } = [];
}
