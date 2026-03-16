namespace Umbraco.AI.Core.Guardrails;

/// <summary>
/// Defines the action to take when a guardrail rule flags content.
/// </summary>
public enum AIGuardrailAction
{
    /// <summary>
    /// Block the content and throw <see cref="AIGuardrailBlockedException"/>.
    /// </summary>
    Block = 0,

    /// <summary>
    /// Allow the content through unchanged and log a warning.
    /// </summary>
    Warn = 1,

    /// <summary>
    /// Replace flagged content with a redaction placeholder before it reaches the AI model or caller.
    /// </summary>
    Redact = 2
}
