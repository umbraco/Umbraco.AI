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
    /// Allow the content through but attach warning metadata to the response.
    /// </summary>
    Warn = 1
}
