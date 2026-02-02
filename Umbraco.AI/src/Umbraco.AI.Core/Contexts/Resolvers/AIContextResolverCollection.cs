using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Contexts.Resolvers;

/// <summary>
/// A collection of context resolvers executed in order to resolve AI context.
/// </summary>
/// <remarks>
/// The order of resolvers in this collection is controlled by the
/// <see cref="AIContextResolverCollectionBuilder"/> using <c>Append</c>, <c>InsertBefore</c>,
/// and <c>InsertAfter</c> methods. Later resolvers can override resources from earlier
/// resolvers when duplicate IDs are encountered.
/// </remarks>
public sealed class AIContextResolverCollection : BuilderCollectionBase<IAiContextResolver>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIContextResolverCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the resolver instances.</param>
    public AIContextResolverCollection(Func<IEnumerable<IAiContextResolver>> items)
        : base(items)
    { }
}
