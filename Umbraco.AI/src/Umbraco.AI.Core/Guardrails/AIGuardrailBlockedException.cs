using Umbraco.AI.Core.Guardrails.Evaluators;

namespace Umbraco.AI.Core.Guardrails;

/// <summary>
/// Exception thrown when a guardrail blocks content.
/// Contains the evaluation results with meaningful messages about why the content was blocked.
/// </summary>
public sealed class AIGuardrailBlockedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIGuardrailBlockedException"/> class.
    /// </summary>
    /// <param name="evaluationResult">The evaluation result that caused the block.</param>
    public AIGuardrailBlockedException(AIGuardrailEvaluationResult evaluationResult)
        : base(BuildMessage(evaluationResult))
    {
        EvaluationResult = evaluationResult;
    }

    /// <summary>
    /// The evaluation result containing details about which rules flagged the content.
    /// </summary>
    public AIGuardrailEvaluationResult EvaluationResult { get; }

    private static string BuildMessage(AIGuardrailEvaluationResult result)
    {
        var summary = result.GetSummaryMessage();
        var phase = result.Phase == AIGuardrailPhase.PreGenerate ? "input" : "response";
        return string.IsNullOrWhiteSpace(summary)
            ? $"The {phase} was blocked by a guardrail policy."
            : $"The {phase} was blocked by a guardrail policy: {summary}";
    }
}
