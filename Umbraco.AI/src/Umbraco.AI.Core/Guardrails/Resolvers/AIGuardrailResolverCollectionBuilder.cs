using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Guardrails.Resolvers;

/// <summary>
/// An ordered collection builder for AI guardrail resolvers.
/// </summary>
/// <remarks>
/// <para>
/// Use this builder to configure the order of guardrail resolution:
/// </para>
/// <code>
/// builder.AIGuardrailResolvers()
///     .Append&lt;ProfileGuardrailResolver&gt;()
///     .InsertAfter&lt;ProfileGuardrailResolver, AgentGuardrailResolver&gt;();
/// </code>
/// </remarks>
public class AIGuardrailResolverCollectionBuilder
    : OrderedCollectionBuilderBase<AIGuardrailResolverCollectionBuilder, AIGuardrailResolverCollection, IAIGuardrailResolver>
{
    /// <inheritdoc />
    protected override AIGuardrailResolverCollectionBuilder This => this;
}
