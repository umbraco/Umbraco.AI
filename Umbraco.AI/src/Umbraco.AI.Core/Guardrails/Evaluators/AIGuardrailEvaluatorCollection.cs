using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Guardrails.Evaluators;

/// <summary>
/// A collection of AI guardrail evaluators.
/// </summary>
public sealed class AIGuardrailEvaluatorCollection : BuilderCollectionBase<IAIGuardrailEvaluator>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIGuardrailEvaluatorCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the evaluator instances.</param>
    public AIGuardrailEvaluatorCollection(Func<IEnumerable<IAIGuardrailEvaluator>> items)
        : base(items)
    { }

    /// <summary>
    /// Gets an evaluator by its unique identifier.
    /// </summary>
    /// <param name="evaluatorId">The evaluator identifier.</param>
    /// <returns>The evaluator, or <c>null</c> if not found.</returns>
    public IAIGuardrailEvaluator? GetById(string evaluatorId)
        => this.FirstOrDefault(e => e.Id.Equals(evaluatorId, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets all evaluators of a specific type.
    /// </summary>
    /// <param name="type">The evaluator type.</param>
    /// <returns>Evaluators of the specified type.</returns>
    public IEnumerable<IAIGuardrailEvaluator> GetByType(AIGuardrailEvaluatorType type)
        => this.Where(e => e.Type == type);
}
