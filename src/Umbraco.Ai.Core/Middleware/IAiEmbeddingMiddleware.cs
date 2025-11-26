using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Core.Middleware;

/// <summary>
/// Defines middleware that can be applied to AI embedding generators.
/// Middleware can implement cross-cutting concerns like logging, caching, rate limiting, etc.
/// </summary>
/// <remarks>
/// The order of middleware execution is controlled by the <see cref="AiEmbeddingMiddlewareCollectionBuilder"/>
/// using <c>Append</c>, <c>InsertBefore</c>, and <c>InsertAfter</c> methods.
/// </remarks>
public interface IAiEmbeddingMiddleware
{
    /// <summary>
    /// Applies this middleware to the given embedding generator.
    /// </summary>
    /// <param name="generator">The embedding generator to wrap with middleware.</param>
    /// <returns>The wrapped embedding generator with middleware applied.</returns>
    IEmbeddingGenerator<string, Embedding<float>> Apply(IEmbeddingGenerator<string, Embedding<float>> generator);
}
