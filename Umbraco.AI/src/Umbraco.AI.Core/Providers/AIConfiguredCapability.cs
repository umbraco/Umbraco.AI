using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;

#pragma warning disable MEAI001 // ISpeechToTextClient / IImageGenerator are experimental in M.E.AI
#pragma warning disable UMBRACOAI_IMAGEGEN // Wraps the experimental image-generation capability

namespace Umbraco.AI.Core.Providers;

/// <summary>
/// Decorator that wraps a chat capability with resolved settings.
/// </summary>
internal sealed class AIConfiguredChatCapability(IAIChatCapability inner, object settings) : IAIConfiguredChatCapability
{
    /// <inheritdoc />
    public AICapability Kind => inner.Kind;

    /// <inheritdoc />
    public Task<IChatClient> CreateClientAsync(string? modelId = null, CancellationToken cancellationToken = default)
        => inner.CreateClientAsync(settings, modelId, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(CancellationToken cancellationToken = default)
        => inner.GetModelsAsync(settings, cancellationToken);
}

/// <summary>
/// Decorator that wraps an embedding capability with resolved settings.
/// </summary>
internal sealed class AIConfiguredEmbeddingCapability(IAIEmbeddingCapability inner, object settings)
    : IAIConfiguredEmbeddingCapability
{
    /// <inheritdoc />
    public AICapability Kind => inner.Kind;

    /// <inheritdoc />
    public Task<IEmbeddingGenerator<string, Embedding<float>>> CreateGeneratorAsync(string? modelId = null, CancellationToken cancellationToken = default)
         => inner.CreateGeneratorAsync(settings, modelId, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(CancellationToken cancellationToken = default)
        => inner.GetModelsAsync(settings, cancellationToken);
}

/// <summary>
/// Decorator that wraps a speech-to-text capability with resolved settings.
/// </summary>
internal sealed class AIConfiguredSpeechToTextCapability(IAISpeechToTextCapability inner, object settings)
    : IAIConfiguredSpeechToTextCapability
{
    /// <inheritdoc />
    public AICapability Kind => inner.Kind;

    /// <inheritdoc />
    public Task<ISpeechToTextClient> CreateClientAsync(string? modelId = null, CancellationToken cancellationToken = default)
        => inner.CreateClientAsync(settings, modelId, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(CancellationToken cancellationToken = default)
        => inner.GetModelsAsync(settings, cancellationToken);
}

/// <summary>
/// Decorator that wraps an image-generation capability with resolved settings.
/// </summary>
internal sealed class AIConfiguredImageGeneratorCapability(IAIImageGeneratorCapability inner, object settings)
    : IAIConfiguredImageGeneratorCapability
{
    /// <inheritdoc />
    public AICapability Kind => inner.Kind;

    /// <inheritdoc />
    public Task<IImageGenerator> CreateGeneratorAsync(string? modelId = null, CancellationToken cancellationToken = default)
        => inner.CreateGeneratorAsync(settings, modelId, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(CancellationToken cancellationToken = default)
        => inner.GetModelsAsync(settings, cancellationToken);
}
