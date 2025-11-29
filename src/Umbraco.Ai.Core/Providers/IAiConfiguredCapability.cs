using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Providers;

/// <summary>
/// Base interface for capabilities with resolved settings.
/// Settings are baked in - no settings parameters needed.
/// </summary>
public interface IAiConfiguredCapability
{
    /// <summary>
    /// Gets the kind of AI capability.
    /// </summary>
    AiCapability Kind { get; }

    /// <summary>
    /// Gets the available AI models for this capability.
    /// </summary>
    Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Chat capability with resolved settings.
/// </summary>
public interface IAiConfiguredChatCapability : IAiConfiguredCapability
{
    /// <summary>
    /// Creates a chat client with the baked-in settings.
    /// </summary>
    IChatClient CreateClient();
}

/// <summary>
/// Embedding capability with resolved settings.
/// </summary>
public interface IAiConfiguredEmbeddingCapability : IAiConfiguredCapability
{
    /// <summary>
    /// Creates an embedding generator with the baked-in settings.
    /// </summary>
    IEmbeddingGenerator<string, Embedding<float>> CreateGenerator();
}
