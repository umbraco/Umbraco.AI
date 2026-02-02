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
    /// <returns>A configured chat client.</returns>
    IChatClient CreateClient(string? modelId = null);
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
    /// <returns>A configured embedding generator.</returns>
    IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(string? modelId = null);
}
