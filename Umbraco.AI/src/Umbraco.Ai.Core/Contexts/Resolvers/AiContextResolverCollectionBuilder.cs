using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Core.Contexts.Resolvers;

/// <summary>
/// An ordered collection builder for AI context resolvers.
/// </summary>
/// <remarks>
/// <para>
/// Use this builder to configure the order of context resolution:
/// </para>
/// <code>
/// builder.AiContextResolvers()
///     .Append&lt;ProfileContextResolver&gt;()
///     .InsertAfter&lt;ProfileContextResolver, AgentContextResolver&gt;();
/// </code>
/// <para>
/// Resolvers are executed in collection order. Later resolvers can override resources
/// from earlier resolvers when duplicate resource IDs are encountered.
/// </para>
/// </remarks>
public class AiContextResolverCollectionBuilder
    : OrderedCollectionBuilderBase<AiContextResolverCollectionBuilder, AiContextResolverCollection, IAiContextResolver>
{
    /// <inheritdoc />
    protected override AiContextResolverCollectionBuilder This => this;
}
