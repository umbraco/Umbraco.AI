using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.Providers;

/// <summary>
/// Base interface for capabilities with resolved settings.
/// Settings are baked in - no settings parameters needed.
/// </summary>
public interface IAIConfiguredCapability
{
    /// <summary>
    /// Gets the kind of AI capability.
    /// </summary>
    AICapability Kind { get; }

    /// <summary>
    /// Gets the available AI models for this capability.
    /// </summary>
    Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Chat capability with resolved settings.
/// </summary>
public interface IAIConfiguredChatCapability : IAIConfiguredCapability
{
    /// <summary>
    /// Creates a chat client with the baked-in settings.
    /// </summary>
    /// <param name="modelId">Optional model ID to use. If null, the provider's default model is used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured chat client.</returns>
    Task<IChatClient> CreateClientAsync(string? modelId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Embedding capability with resolved settings.
/// </summary>
public interface IAIConfiguredEmbeddingCapability : IAIConfiguredCapability
{
    /// <summary>
    /// Creates an embedding generator with the baked-in settings.
    /// </summary>
    /// <param name="modelId">Optional model ID to use. If null, the provider's default model is used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured embedding generator.</returns>
    Task<IEmbeddingGenerator<string, Embedding<float>>> CreateGeneratorAsync(string? modelId = null, CancellationToken cancellationToken = default);
}
