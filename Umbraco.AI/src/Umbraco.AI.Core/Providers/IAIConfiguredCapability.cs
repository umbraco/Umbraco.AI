using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.ImageGeneration;
using Umbraco.AI.Core.Models;

#pragma warning disable MEAI001 // ISpeechToTextClient / IImageGenerator are experimental in M.E.AI
#pragma warning disable UMBRACOAI_IMAGEGEN // Defining the experimental image-generation capability surface

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

    /// <summary>
    /// Creates a chat client with the baked-in connection settings and resolved, provider-declared
    /// profile settings (e.g. reasoning effort).
    /// </summary>
    /// <param name="capabilitySettings">The resolved, typed profile settings, or <c>null</c> when the profile declares none.</param>
    /// <param name="modelId">Optional model ID to use. If null, the provider's default model is used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured chat client.</returns>
    /// <remarks>
    /// Default implementation ignores <paramref name="capabilitySettings"/> and delegates to
    /// <see cref="CreateClientAsync(string?, CancellationToken)"/> so existing callers/implementations keep working.
    /// </remarks>
    Task<IChatClient> CreateClientAsync(object? capabilitySettings, string? modelId, CancellationToken cancellationToken)
        => CreateClientAsync(modelId, cancellationToken);
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

/// <summary>
/// Speech-to-text capability with resolved settings.
/// </summary>
public interface IAIConfiguredSpeechToTextCapability : IAIConfiguredCapability
{
    /// <summary>
    /// Creates a speech-to-text client with the baked-in settings.
    /// </summary>
    /// <param name="modelId">Optional model ID to use. If null, the provider's default model is used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured speech-to-text client.</returns>
    Task<ISpeechToTextClient> CreateClientAsync(string? modelId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Image-generation capability with resolved settings.
/// </summary>
[Experimental(AIImageGenerationDiagnostics.DiagnosticId)]
public interface IAIConfiguredImageGeneratorCapability : IAIConfiguredCapability
{
    /// <summary>
    /// Creates an image generator with the baked-in settings.
    /// </summary>
    /// <param name="modelId">Optional model ID to use. If null, the provider's default model is used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured image generator.</returns>
    Task<IImageGenerator> CreateGeneratorAsync(string? modelId = null, CancellationToken cancellationToken = default);
}
