namespace Umbraco.Ai.Prompt.Core.Prompts;

/// <summary>
/// Defines where a prompt is allowed to run, both for UI display and server-side enforcement.
/// </summary>
public class AiPromptScope
{
    /// <summary>
    /// Rules that define where the prompt is allowed to run.
    /// If any rule matches, the prompt can execute (OR logic between rules).
    /// Empty means the prompt is not allowed anywhere.
    /// </summary>
    public IReadOnlyList<AiPromptScopeRule> AllowRules { get; set; } = [];

    /// <summary>
    /// Rules that define where the prompt is explicitly denied.
    /// If any rule matches, the prompt cannot execute (OR logic between rules).
    /// Deny rules take precedence over allow rules.
    /// </summary>
    public IReadOnlyList<AiPromptScopeRule> DenyRules { get; set; } = [];
}
