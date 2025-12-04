namespace Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Models;

/// <summary>
/// API model for prompt scope configuration.
/// </summary>
public class ScopeModel
{
    /// <summary>
    /// Rules that define where the prompt should appear (whitelist).
    /// If any rule matches, the prompt is included (OR logic between rules).
    /// Empty means the prompt appears nowhere.
    /// </summary>
    public IEnumerable<ScopeRuleModel> IncludeRules { get; set; } = [];

    /// <summary>
    /// Rules that define where the prompt should NOT appear (blacklist).
    /// If any rule matches, the prompt is excluded (OR logic between rules).
    /// Exclusions take precedence over inclusions.
    /// </summary>
    public IEnumerable<ScopeRuleModel> ExcludeRules { get; set; } = [];
}
