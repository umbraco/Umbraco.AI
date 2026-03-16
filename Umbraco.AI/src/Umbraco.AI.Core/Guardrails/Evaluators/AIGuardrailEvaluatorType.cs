namespace Umbraco.AI.Core.Guardrails.Evaluators;

/// <summary>
/// Defines the type of guardrail evaluator.
/// </summary>
public enum AIGuardrailEvaluatorType
{
    /// <summary>
    /// A fast, deterministic evaluator using code-based logic (regex, keywords, patterns).
    /// Suitable for real-time streaming evaluation.
    /// </summary>
    CodeBased = 0,

    /// <summary>
    /// An evaluator that uses an AI model for inference-based evaluation (e.g., LLM-as-judge).
    /// Only runs after the full response is available (not during streaming chunks).
    /// </summary>
    ModelBased = 1
}
