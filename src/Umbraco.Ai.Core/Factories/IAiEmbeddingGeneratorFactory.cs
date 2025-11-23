using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Factories;

/// <summary>
/// Factory for creating configured IEmbeddingGenerator instances.
/// Handles generator creation from providers and middleware application.
/// </summary>
public interface IAiEmbeddingGeneratorFactory
{
    /// <summary>
    /// Creates a fully configured embedding generator for the given profile.
    /// </summary>
    /// <param name="profile">The AI profile containing model and connection information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured IEmbeddingGenerator with all middleware applied.</returns>
    Task<IEmbeddingGenerator<string, Embedding<float>>> CreateGeneratorAsync(
        AiProfile profile,
        CancellationToken cancellationToken = default);
}
