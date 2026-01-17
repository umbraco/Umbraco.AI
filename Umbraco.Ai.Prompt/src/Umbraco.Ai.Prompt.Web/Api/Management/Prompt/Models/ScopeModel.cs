namespace Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Models;

/// <summary>
/// API model for prompt scope configuration.
/// </summary>
public class ScopeModel
{
    /// <summary>
    /// Rules that define where the prompt is allowed to run.
    /// If any rule matches, the prompt can execute (OR logic between rules).
    /// Empty means the prompt is not allowed anywhere.
    /// </summary>
    public IEnumerable<ScopeRuleModel> AllowRules { get; set; } = [];

    /// <summary>
    /// Rules that define where the prompt is explicitly denied.
    /// If any rule matches, the prompt cannot execute (OR logic between rules).
    /// Deny rules take precedence over allow rules.
    /// </summary>
    public IEnumerable<ScopeRuleModel> DenyRules { get; set; } = [];
}
