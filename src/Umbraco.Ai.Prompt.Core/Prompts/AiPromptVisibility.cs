namespace Umbraco.Ai.Prompt.Core.Prompts;

/// <summary>
/// Defines where a prompt should appear as a property action.
/// </summary>
public class AiPromptVisibility
{
    /// <summary>
    /// Rules that define where the prompt should appear.
    /// If any rule matches, the prompt is shown (OR logic between rules).
    /// Empty means the prompt appears nowhere.
    /// </summary>
    public IReadOnlyList<AiPromptVisibilityRule> ShowRules { get; set; } = [];

    /// <summary>
    /// Rules that define where the prompt should NOT appear.
    /// If any rule matches, the prompt is hidden (OR logic between rules).
    /// Hide rules take precedence over show rules.
    /// </summary>
    public IReadOnlyList<AiPromptVisibilityRule> HideRules { get; set; } = [];
}
