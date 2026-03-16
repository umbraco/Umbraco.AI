using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Guardrails.Resolvers;

/// <summary>
/// A collection of guardrail resolvers executed in order to resolve applicable guardrails.
/// </summary>
public sealed class AIGuardrailResolverCollection : BuilderCollectionBase<IAIGuardrailResolver>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIGuardrailResolverCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the resolver instances.</param>
    public AIGuardrailResolverCollection(Func<IEnumerable<IAIGuardrailResolver>> items)
        : base(items)
    { }
}
