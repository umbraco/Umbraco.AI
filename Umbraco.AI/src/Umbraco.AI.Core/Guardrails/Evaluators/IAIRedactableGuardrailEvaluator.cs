namespace Umbraco.AI.Core.Guardrails.Evaluators;

/// <summary>
/// Implemented by guardrail evaluators that can identify specific text spans for redaction.
/// </summary>
public interface IAIRedactableGuardrailEvaluator
{
    /// <summary>
    /// Finds all redactable matches in the content.
    /// </summary>
    /// <param name="content">The text content to scan for redactable matches.</param>
    /// <param name="config">The evaluator-specific configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of redactable matches with their positions.</returns>
    Task<IReadOnlyList<AIGuardrailRedactionCandidate>> FindRedactionCandidatesAsync(
        string content,
        AIGuardrailConfig config,
        CancellationToken cancellationToken);
}
