namespace Umbraco.Ai.Agent.Web.Api.Management.Agent.Models;

/// <summary>
/// API model for agent scope configuration.
/// </summary>
public class ScopeModel
{
    /// <summary>
    /// Rules that define where the agent is allowed to run.
    /// If any rule matches, the agent can execute (OR logic between rules).
    /// Empty means the agent is not allowed anywhere.
    /// </summary>
    public IEnumerable<ScopeRuleModel> AllowRules { get; set; } = [];

    /// <summary>
    /// Rules that define where the agent is explicitly denied.
    /// If any rule matches, the agent cannot execute (OR logic between rules).
    /// Deny rules take precedence over allow rules.
    /// </summary>
    public IEnumerable<ScopeRuleModel> DenyRules { get; set; } = [];
}
