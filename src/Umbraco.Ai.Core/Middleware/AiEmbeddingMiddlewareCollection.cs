using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Core.Middleware;

/// <summary>
/// A collection of embedding middleware applied in order to AI embedding generators.
/// </summary>
/// <remarks>
/// The order of middleware in this collection is controlled by the
/// <see cref="AiEmbeddingMiddlewareCollectionBuilder"/> using <c>Append</c>, <c>InsertBefore</c>,
/// and <c>InsertAfter</c> methods.
/// </remarks>
public sealed class AiEmbeddingMiddlewareCollection : BuilderCollectionBase<IAiEmbeddingMiddleware>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiEmbeddingMiddlewareCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the middleware instances.</param>
    public AiEmbeddingMiddlewareCollection(Func<IEnumerable<IAiEmbeddingMiddleware>> items)
        : base(items)
    { }
}
