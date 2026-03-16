namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Options for controlling prompt execution behavior.
/// </summary>
public class AIPromptExecutionOptions
{
    /// <summary>
    /// Whether to validate scope rules before execution. Default is true.
    /// Set to false for test execution where scope validation is not relevant.
    /// </summary>
    public bool ValidateScope { get; init; } = true;

    /// <summary>
    /// Optional profile ID to override the prompt's configured profile.
    /// Used for cross-model comparison testing.
    /// </summary>
    public Guid? ProfileIdOverride { get; init; }

    /// <summary>
    /// Optional context IDs to override the prompt's configured <see cref="AIPrompt.ContextIds"/>.
    /// Used for context comparison testing.
    /// </summary>
    public IReadOnlyList<Guid>? ContextIdsOverride { get; init; }

    /// <summary>
    /// Optional guardrail IDs to override for testing guardrail behavior.
    /// When set, this value is stored in the runtime context for guardrail resolvers to pick up.
    /// </summary>
    public IReadOnlyList<Guid>? GuardrailIdsOverride { get; init; }
}
