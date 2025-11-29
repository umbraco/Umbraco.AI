using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Providers;

/// <summary>
/// Decorator that wraps a chat capability with resolved settings.
/// </summary>
internal sealed class ConfiguredChatCapability : IConfiguredChatCapability
{
    private readonly IAiChatCapability _inner;
    private readonly object _settings;

    public ConfiguredChatCapability(IAiChatCapability inner, object settings)
    {
        _inner = inner;
        _settings = settings;
    }

    /// <inheritdoc />
    public AiCapability Kind => _inner.Kind;

    /// <inheritdoc />
    public IChatClient CreateClient() => _inner.CreateClient(_settings);

    /// <inheritdoc />
    public Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(CancellationToken cancellationToken = default)
        => _inner.GetModelsAsync(_settings, cancellationToken);
}

/// <summary>
/// Decorator that wraps an embedding capability with resolved settings.
/// </summary>
internal sealed class ConfiguredEmbeddingCapability : IConfiguredEmbeddingCapability
{
    private readonly IAiEmbeddingCapability _inner;
    private readonly object _settings;

    public ConfiguredEmbeddingCapability(IAiEmbeddingCapability inner, object settings)
    {
        _inner = inner;
        _settings = settings;
    }

    /// <inheritdoc />
    public AiCapability Kind => _inner.Kind;

    /// <inheritdoc />
    public IEmbeddingGenerator<string, Embedding<float>> CreateGenerator()
        => _inner.CreateGenerator(_settings);

    /// <inheritdoc />
    public Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(CancellationToken cancellationToken = default)
        => _inner.GetModelsAsync(_settings, cancellationToken);
}
