using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Core.Providers;

namespace Umbraco.Ai.Core.Embeddings;

internal sealed class AiEmbeddingGeneratorFactory : IAiEmbeddingGeneratorFactory
{
    private readonly IAiConnectionService _connectionService;
    private readonly AiEmbeddingMiddlewareCollection _middleware;

    public AiEmbeddingGeneratorFactory(
        IAiConnectionService connectionService,
        AiEmbeddingMiddlewareCollection middleware)
    {
        _connectionService = connectionService;
        _middleware = middleware;
    }

    public async Task<IEmbeddingGenerator<string, Embedding<float>>> CreateGeneratorAsync(
        AiProfile profile,
        CancellationToken cancellationToken = default)
    {
        // Get configured provider with resolved settings
        var embeddingCapability = await GetConfiguredEmbeddingCapabilityAsync(profile, cancellationToken);

        // Create base generator from provider (settings already resolved)
        var generator = embeddingCapability.CreateGenerator();

        // Apply middleware in order
        generator = ApplyMiddleware(generator);

        return generator;
    }

    private IEmbeddingGenerator<string, Embedding<float>> ApplyMiddleware(
        IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        // Apply middleware in collection order (controlled by AiEmbeddingMiddlewareCollectionBuilder)
        foreach (var middleware in _middleware)
        {
            generator = middleware.Apply(generator);
        }

        return generator;
    }

    private async Task<IConfiguredEmbeddingCapability> GetConfiguredEmbeddingCapabilityAsync(
        AiProfile profile,
        CancellationToken cancellationToken)
    {
        if (profile.ConnectionId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Profile '{profile.Name}' does not specify a valid ConnectionId.");
        }

        var configured = await _connectionService.GetConfiguredProviderAsync(
            profile.ConnectionId,
            cancellationToken);

        if (configured is null)
        {
            throw new InvalidOperationException(
                $"Connection with ID '{profile.ConnectionId}' not found for profile '{profile.Name}'.");
        }

        if (!configured.Connection.IsActive)
        {
            throw new InvalidOperationException(
                $"Connection '{configured.Connection.Name}' (ID: {profile.ConnectionId}) is not active.");
        }

        // Validate connection provider matches profile's model provider
        if (!string.Equals(configured.Provider.Id, profile.Model.ProviderId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Connection is for provider '{configured.Provider.Id}' " +
                $"but profile '{profile.Name}' requires provider '{profile.Model.ProviderId}'.");
        }

        var embeddingCapability = configured.GetCapability<IConfiguredEmbeddingCapability>();
        if (embeddingCapability is null)
        {
            throw new InvalidOperationException(
                $"Provider '{profile.Model.ProviderId}' does not support embedding capability.");
        }

        return embeddingCapability;
    }
}
