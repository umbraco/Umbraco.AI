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
    [Obsolete("Use GenerateEmbeddingAsync with builder overload for full observability. Will be removed in v3.")]
    Task<Embedding<float>> GenerateEmbeddingAsync(
        string value,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an embedding for a single text value using a specific profile.
    /// </summary>
    [Obsolete("Use GenerateEmbeddingAsync with .WithProfile(profileId) for full observability. Will be removed in v3.")]
    Task<Embedding<float>> GenerateEmbeddingAsync(
        Guid profileId,
        string value,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for multiple text values using the default embedding profile.
    /// </summary>
    [Obsolete("Use GenerateEmbeddingsAsync with builder overload for full observability. Will be removed in v3.")]
    Task<GeneratedEmbeddings<Embedding<float>>> GenerateEmbeddingsAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for multiple text values using a specific profile.
    /// </summary>
    [Obsolete("Use GenerateEmbeddingsAsync with .WithProfile(profileId) for full observability. Will be removed in v3.")]
    Task<GeneratedEmbeddings<Embedding<float>>> GenerateEmbeddingsAsync(
        Guid profileId,
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a configured embedding generator for advanced scenarios.
    /// </summary>
    [Obsolete("Use CreateEmbeddingGeneratorAsync with builder overload for per-call scope management. Will be removed in v3.")]
    Task<IEmbeddingGenerator<string, Embedding<float>>> GetEmbeddingGeneratorAsync(
        Guid? profileId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an embedding for a single text value using a builder with full observability
    /// (notifications, telemetry, duration tracking).
    /// </summary>
    /// <param name="configure">Action to configure the inline embedding via the builder.</param>
    /// <param name="value">The text value to generate an embedding for.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The generated embedding vector.</returns>
    Task<Embedding<float>> GenerateEmbeddingAsync(
        Action<AIEmbeddingBuilder> configure,
        string value,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for multiple text values using a builder with full observability
    /// (notifications, telemetry, duration tracking).
    /// </summary>
    /// <param name="configure">Action to configure the inline embedding via the builder.</param>
    /// <param name="values">The text values to generate embeddings for.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The generated embeddings.</returns>
    Task<GeneratedEmbeddings<Embedding<float>>> GenerateEmbeddingsAsync(
        Action<AIEmbeddingBuilder> configure,
        IEnumerable<string> values,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a reusable inline embedding generator with scope management per-call.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Note:</strong> Calling methods on the returned generator does not publish
    /// <see cref="AIEmbeddingExecutingNotification"/> or <see cref="AIEmbeddingExecutedNotification"/>.
    /// Use <see cref="GenerateEmbeddingAsync"/> or <see cref="GenerateEmbeddingsAsync"/>
    /// for notification support.
    /// </para>
    /// </remarks>
    /// <param name="configure">Action to configure the inline embedding via the builder.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A configured IEmbeddingGenerator with inline embedding scope management.</returns>
    Task<IEmbeddingGenerator<string, Embedding<float>>> CreateEmbeddingGeneratorAsync(
        Action<AIEmbeddingBuilder> configure,
        CancellationToken cancellationToken = default);
}
