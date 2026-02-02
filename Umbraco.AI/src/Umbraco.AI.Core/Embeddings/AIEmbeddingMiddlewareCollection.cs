using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Embeddings;

/// <summary>
/// A collection of embedding middleware applied in order to AI embedding generators.
/// </summary>
/// <remarks>
/// The order of middleware in this collection is controlled by the
/// <see cref="AIEmbeddingMiddlewareCollectionBuilder"/> using <c>Append</c>, <c>InsertBefore</c>,
/// and <c>InsertAfter</c> methods.
/// </remarks>
public sealed class AIEmbeddingMiddlewareCollection : BuilderCollectionBase<IAIEmbeddingMiddleware>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIEmbeddingMiddlewareCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the middleware instances.</param>
    public AIEmbeddingMiddlewareCollection(Func<IEnumerable<IAIEmbeddingMiddleware>> items)
        : base(items)
    { }
}
