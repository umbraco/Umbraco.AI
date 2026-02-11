namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Models;

/// <summary>
/// API model for agent scope.
/// Defines where an agent is available using allow and deny rules.
/// </summary>
public class AIAgentScopeModel
{
    /// <summary>
    /// Rules that define where the agent is available.
    /// If any rule matches, the agent can be used (OR logic between rules).
    /// Empty means the agent is available everywhere (unless denied).
    /// </summary>
    public IReadOnlyList<AIAgentScopeRuleModel> AllowRules { get; set; } = [];

    /// <summary>
    /// Rules that define where the agent is explicitly denied.
    /// If any rule matches, the agent cannot be used (OR logic between rules).
    /// Deny rules take precedence over allow rules.
    /// </summary>
    public IReadOnlyList<AIAgentScopeRuleModel> DenyRules { get; set; } = [];
}
