namespace Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Models;

/// <summary>
/// API model for prompt visibility configuration.
/// </summary>
public class VisibilityModel
{
    /// <summary>
    /// Rules that define where the prompt should appear.
    /// If any rule matches, the prompt is shown (OR logic between rules).
    /// Empty means the prompt appears nowhere.
    /// </summary>
    public IEnumerable<VisibilityRuleModel> ShowRules { get; set; } = [];

    /// <summary>
    /// Rules that define where the prompt should NOT appear.
    /// If any rule matches, the prompt is hidden (OR logic between rules).
    /// Hide rules take precedence over show rules.
    /// </summary>
    public IEnumerable<VisibilityRuleModel> HideRules { get; set; } = [];
}
