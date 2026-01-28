using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Providers;

/// <summary>
/// Decorator that wraps a chat capability with resolved settings.
/// </summary>
internal sealed class AiConfiguredChatCapability(IAiChatCapability inner, object settings) : IAiConfiguredChatCapability
{
    /// <inheritdoc />
    public AiCapability Kind => inner.Kind;

    /// <inheritdoc />
    public IChatClient CreateClient(string? modelId = null) => inner.CreateClient(settings, modelId);

    /// <inheritdoc />
    public Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(CancellationToken cancellationToken = default)
        => inner.GetModelsAsync(settings, cancellationToken);
}

/// <summary>
/// Decorator that wraps an embedding capability with resolved settings.
/// </summary>
internal sealed class AiConfiguredEmbeddingCapability(IAiEmbeddingCapability inner, object settings)
    : IAiConfiguredEmbeddingCapability
{
    /// <inheritdoc />
    public AiCapability Kind => inner.Kind;

    /// <inheritdoc />
    public IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(string? modelId = null)
        => inner.CreateGenerator(settings, modelId);

    /// <inheritdoc />
    public Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(CancellationToken cancellationToken = default)
        => inner.GetModelsAsync(settings, cancellationToken);
}
