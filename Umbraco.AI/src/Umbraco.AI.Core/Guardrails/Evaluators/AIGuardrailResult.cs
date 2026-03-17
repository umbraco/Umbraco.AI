using System.Text.Json;

namespace Umbraco.AI.Core.Guardrails.Evaluators;

/// <summary>
/// Result from a single guardrail evaluator.
/// </summary>
public sealed class AIGuardrailResult
{
    /// <summary>
    /// The ID of the evaluator that produced this result.
    /// </summary>
    public required string EvaluatorId { get; init; }

    /// <summary>
    /// Whether the content was flagged by this evaluator.
    /// </summary>
    public required bool Flagged { get; init; }

    /// <summary>
    /// Confidence score from the evaluator (0-1).
    /// Code-based evaluators typically return 0 or 1.
    /// Model-based evaluators return continuous scores.
    /// </summary>
    public double? Score { get; init; }

    /// <summary>
    /// Human-readable explanation of why the content was flagged (or allowed).
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Evaluator-specific metadata (e.g., matched patterns, LLM reasoning).
    /// </summary>
    public JsonElement? Metadata { get; init; }
}
