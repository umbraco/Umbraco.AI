namespace Umbraco.Ai.Prompt.Core.Prompts;

/// <summary>
/// Defines where a prompt should appear as a property action.
/// </summary>
public class AiPromptScope
{
    /// <summary>
    /// Rules that define where the prompt should appear (whitelist).
    /// If any rule matches, the prompt is included (OR logic between rules).
    /// Empty means the prompt appears nowhere.
    /// </summary>
    public IReadOnlyList<AiPromptScopeRule> IncludeRules { get; set; } = [];

    /// <summary>
    /// Rules that define where the prompt should NOT appear (blacklist).
    /// If any rule matches, the prompt is excluded (OR logic between rules).
    /// Exclusions take precedence over inclusions.
    /// </summary>
    public IReadOnlyList<AiPromptScopeRule> ExcludeRules { get; set; } = [];
}
