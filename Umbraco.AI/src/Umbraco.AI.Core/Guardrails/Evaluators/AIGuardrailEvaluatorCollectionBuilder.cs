using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Guardrails.Evaluators;

/// <summary>
/// A lazy collection builder for AI guardrail evaluators.
/// </summary>
/// <remarks>
/// Evaluators are auto-discovered via <see cref="IDiscoverable"/> and the <see cref="AIGuardrailEvaluatorAttribute"/>.
/// Use <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Add{T}"/> to add evaluators manually,
/// or <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Exclude{T}"/> to exclude auto-discovered evaluators.
/// </remarks>
public class AIGuardrailEvaluatorCollectionBuilder
    : LazyCollectionBuilderBase<AIGuardrailEvaluatorCollectionBuilder, AIGuardrailEvaluatorCollection, IAIGuardrailEvaluator>
{
    /// <inheritdoc />
    protected override AIGuardrailEvaluatorCollectionBuilder This => this;
}
