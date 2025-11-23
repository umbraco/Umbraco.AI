using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Core.Middleware;

/// <summary>
/// Defines middleware that can be applied to AI embedding generators.
/// Middleware can implement cross-cutting concerns like logging, caching, rate limiting, etc.
/// </summary>
public interface IAiEmbeddingMiddleware
{
    /// <summary>
    /// Applies this middleware to the given embedding generator.
    /// </summary>
    /// <param name="generator">The embedding generator to wrap with middleware.</param>
    /// <returns>The wrapped embedding generator with middleware applied.</returns>
    IEmbeddingGenerator<string, Embedding<float>> Apply(IEmbeddingGenerator<string, Embedding<float>> generator);

    /// <summary>
    /// Gets the order in which this middleware should be applied.
    /// Lower values are applied first (closer to the provider).
    /// Higher values are applied last (closer to the caller).
    /// </summary>
    int Order { get; }
}
