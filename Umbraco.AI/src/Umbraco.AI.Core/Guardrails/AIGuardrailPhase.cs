namespace Umbraco.AI.Core.Guardrails;

/// <summary>
/// Defines when a guardrail rule is evaluated in the chat pipeline.
/// </summary>
public enum AIGuardrailPhase
{
    /// <summary>
    /// Evaluated before the request is sent to the AI provider.
    /// Used for input filtering (e.g., blocking PII in user messages).
    /// </summary>
    PreGenerate = 0,

    /// <summary>
    /// Evaluated after the AI provider returns a response.
    /// Used for output validation (e.g., blocking misinformation, toxic content).
    /// </summary>
    PostGenerate = 1
}
