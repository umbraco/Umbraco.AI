namespace Umbraco.AI.Core.Guardrails.Evaluators;

/// <summary>
/// Aggregate result from evaluating content against all guardrail rules.
/// </summary>
public sealed class AIGuardrailEvaluationResult
{
    /// <summary>
    /// The overall action determined by the evaluation.
    /// Precedence: Block &gt; Redact &gt; Warn.
    /// </summary>
    public required AIGuardrailAction Action { get; init; }

    /// <summary>
    /// The phase that was evaluated.
    /// </summary>
    public required AIGuardrailPhase Phase { get; init; }

    /// <summary>
    /// Individual results from each evaluator that was run.
    /// </summary>
    public required IReadOnlyList<AIGuardrailRuleResult> RuleResults { get; init; }

    /// <summary>
    /// Whether any evaluator flagged the content.
    /// </summary>
    public bool HasFlaggedContent => RuleResults.Any(r => r.EvaluatorResult.Flagged);

    /// <summary>
    /// Gets a summary message describing why content was blocked or warned.
    /// </summary>
    public string? GetSummaryMessage()
    {
        var flaggedResults = RuleResults.Where(r => r.EvaluatorResult.Flagged).ToList();
        if (flaggedResults.Count == 0)
        {
            return null;
        }

        var names = flaggedResults
            .Select(r => string.IsNullOrWhiteSpace(r.Rule.GuardrailName)
                ? r.Rule.Name
                : $"{r.Rule.GuardrailName} > {r.Rule.Name}");

        return string.Join("; ", names);
    }
}

/// <summary>
/// Result from evaluating a single guardrail rule.
/// </summary>
public sealed class AIGuardrailRuleResult
{
    /// <summary>
    /// The rule that was evaluated.
    /// </summary>
    public required AIGuardrailRule Rule { get; init; }

    /// <summary>
    /// The result from the evaluator.
    /// </summary>
    public required AIGuardrailResult EvaluatorResult { get; init; }
}
