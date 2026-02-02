using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.Providers;

/// <summary>
/// Decorator that wraps a chat capability with resolved settings.
/// </summary>
internal sealed class AIConfiguredChatCapability(IAiChatCapability inner, object settings) : IAiConfiguredChatCapability
{
    /// <inheritdoc />
    public AICapability Kind => inner.Kind;

    /// <inheritdoc />
    public IChatClient CreateClient(string? modelId = null) => inner.CreateClient(settings, modelId);

    /// <inheritdoc />
    public Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(CancellationToken cancellationToken = default)
        => inner.GetModelsAsync(settings, cancellationToken);
}

/// <summary>
/// Decorator that wraps an embedding capability with resolved settings.
/// </summary>
internal sealed class AIConfiguredEmbeddingCapability(IAiEmbeddingCapability inner, object settings)
    : IAiConfiguredEmbeddingCapability
{
    /// <inheritdoc />
    public AICapability Kind => inner.Kind;

    /// <inheritdoc />
    public IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(string? modelId = null)
        => inner.CreateGenerator(settings, modelId);

    /// <inheritdoc />
    public Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(CancellationToken cancellationToken = default)
        => inner.GetModelsAsync(settings, cancellationToken);
}
