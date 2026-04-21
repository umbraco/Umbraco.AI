using System.Text.Json;
using Umbraco.AI.Core.Guardrails.Evaluators;

namespace Umbraco.AI.Core.Guardrails;

/// <summary>
/// Represents a single rule within an <see cref="AIGuardrail"/>.
/// Each rule references a registered evaluator and specifies when and how it is applied.
/// </summary>
public sealed class AIGuardrailRule
{
    /// <summary>
    /// The unique identifier of the rule.
    /// </summary>
    public Guid Id { get; internal set; }

    /// <summary>
    /// The identifier of the registered evaluator to use (e.g., "pii", "toxicity", "llm-judge").
    /// Must match <see cref="IAIGuardrailEvaluator.Id"/>.
    /// </summary>
    public required string EvaluatorId { get; init; }

    /// <summary>
    /// The display name of the rule.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The phase in which this rule is evaluated.
    /// </summary>
    public AIGuardrailPhase Phase { get; set; } = AIGuardrailPhase.PostGenerate;

    /// <summary>
    /// The action to take when this rule flags content.
    /// </summary>
    public AIGuardrailAction Action { get; set; } = AIGuardrailAction.Block;

    /// <summary>
    /// Evaluator-specific configuration as a JSON element.
    /// The schema is defined by the evaluator's <see cref="IAIGuardrailEvaluator.GetConfigSchema"/>.
    /// </summary>
    public JsonElement? Config { get; set; }

    /// <summary>
    /// The name of the parent guardrail this rule belongs to.
    /// Set during resolution to provide context in evaluation results.
    /// </summary>
    public string? GuardrailName { get; set; }

    /// <summary>
    /// The ID of the parent guardrail this rule belongs to. Populated during resolution so the
    /// aggregator can dedupe rules whose guardrail was already contributed by an earlier resolver.
    /// Distinct from <see cref="Id"/>, which is the rule's own identifier.
    /// </summary>
    public Guid? GuardrailId { get; set; }

    /// <summary>
    /// Controls evaluation order within the guardrail.
    /// </summary>
    public int SortOrder { get; set; }
}
