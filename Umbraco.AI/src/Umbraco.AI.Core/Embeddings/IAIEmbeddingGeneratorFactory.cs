using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Profiles;

namespace Umbraco.AI.Core.Embeddings;

/// <summary>
/// Factory for creating configured IEmbeddingGenerator instances.
/// Handles generator creation from providers and middleware application.
/// </summary>
public interface IAIEmbeddingGeneratorFactory
{
    /// <summary>
    /// Creates a fully configured embedding generator for the given profile.
    /// </summary>
    /// <param name="profile">The AI profile containing model and connection information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured IEmbeddingGenerator with all middleware applied.</returns>
    Task<IEmbeddingGenerator<string, Embedding<float>>> CreateGeneratorAsync(
        AIProfile profile,
        CancellationToken cancellationToken = default);
}
