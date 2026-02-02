using Microsoft.Extensions.AI;

namespace Umbraco.AI.Core.Embeddings;

/// <summary>
/// Defines an AI embedding service that provides access to text embedding capabilities.
/// This service acts as a thin layer over Microsoft.Extensions.AI, adding Umbraco-specific
/// features like profiles, connections, and configurable middleware.
/// </summary>
public interface IAIEmbeddingService
{
    /// <summary>
    /// Generates an embedding for a single text value using the default embedding profile.
    /// </summary>
    /// <param name="value">The text value to generate an embedding for.</param>
    /// <param name="options">Optional embedding generation options.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The generated embedding vector.</returns>
    Task<Embedding<float>> GenerateEmbeddingAsync(
        string value,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an embedding for a single text value using a specific profile.
    /// </summary>
    /// <param name="profileId">The ID of the profile to use.</param>
    /// <param name="value">The text value to generate an embedding for.</param>
    /// <param name="options">Optional embedding generation options.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The generated embedding vector.</returns>
    Task<Embedding<float>> GenerateEmbeddingAsync(
        Guid profileId,
        string value,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for multiple text values using the default embedding profile.
    /// </summary>
    /// <param name="values">The text values to generate embeddings for.</param>
    /// <param name="options">Optional embedding generation options.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The generated embeddings.</returns>
    Task<GeneratedEmbeddings<Embedding<float>>> GenerateEmbeddingsAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for multiple text values using a specific profile.
    /// </summary>
    /// <param name="profileId">The ID of the profile to use.</param>
    /// <param name="values">The text values to generate embeddings for.</param>
    /// <param name="options">Optional embedding generation options.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The generated embeddings.</returns>
    Task<GeneratedEmbeddings<Embedding<float>>> GenerateEmbeddingsAsync(
        Guid profileId,
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a configured embedding generator for advanced scenarios.
    /// The returned generator has all registered middleware applied and is configured
    /// according to the specified profile.
    /// </summary>
    /// <param name="profileId">Optional profile id. If not specified, uses the default embedding profile.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A configured IEmbeddingGenerator instance with middleware applied.</returns>
    Task<IEmbeddingGenerator<string, Embedding<float>>> GetEmbeddingGeneratorAsync(
        Guid? profileId = null,
        CancellationToken cancellationToken = default);
}
