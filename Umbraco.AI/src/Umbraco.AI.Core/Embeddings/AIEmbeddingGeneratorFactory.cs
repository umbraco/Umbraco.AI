using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Core.Embeddings;

internal sealed class AIEmbeddingGeneratorFactory : IAIEmbeddingGeneratorFactory
{
    private readonly IAIConnectionService _connectionService;
    private readonly AIEmbeddingMiddlewareCollection _middleware;

    public AIEmbeddingGeneratorFactory(
        IAIConnectionService connectionService,
        AIEmbeddingMiddlewareCollection middleware)
    {
        _connectionService = connectionService;
        _middleware = middleware;
    }

    public async Task<IEmbeddingGenerator<string, Embedding<float>>> CreateGeneratorAsync(
        AIProfile profile,
        CancellationToken cancellationToken = default)
    {
        // Get configured provider with resolved settings
        var embeddingCapability = await GetConfiguredEmbeddingCapabilityAsync(profile, cancellationToken);

        // Create base generator from provider with the profile's model
        var generator = embeddingCapability.CreateGenerator(profile.Model.ModelId);

        // Apply middleware in order
        generator = ApplyMiddleware(generator);

        return generator;
    }

    private IEmbeddingGenerator<string, Embedding<float>> ApplyMiddleware(
        IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        // Apply middleware in collection order (controlled by AIEmbeddingMiddlewareCollectionBuilder)
        foreach (var middleware in _middleware)
        {
            generator = middleware.Apply(generator);
        }

        return generator;
    }

    private async Task<IAIConfiguredEmbeddingCapability> GetConfiguredEmbeddingCapabilityAsync(
        AIProfile profile,
        CancellationToken cancellationToken)
    {
        if (profile.ConnectionId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Profile '{profile.Name}' does not specify a valid ConnectionId.");
        }
        
        var connection = await _connectionService.GetConnectionAsync(
            profile.ConnectionId,
            cancellationToken);
        if (connection is null)
        {
            throw new InvalidOperationException(
                $"Connection with ID '{profile.ConnectionId}' not found for profile '{profile.Name}'.");
        }

        if (!connection.IsActive)
        {
            throw new InvalidOperationException(
                $"Connection '{connection.Name}' (ID: {profile.ConnectionId}) is not active.");
        }

        var configured = await _connectionService.GetConfiguredProviderAsync(
            profile.ConnectionId,
            cancellationToken);

        if (configured is null)
        {
            throw new InvalidOperationException(
                $"Connection with ID '{profile.ConnectionId}' not found for profile '{profile.Name}'.");
        }

        // Validate connection provider matches profile's model provider
        if (!string.Equals(configured.Provider.Id, profile.Model.ProviderId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Connection is for provider '{configured.Provider.Id}' " +
                $"but profile '{profile.Name}' requires provider '{profile.Model.ProviderId}'.");
        }

        var embeddingCapability = configured.GetCapability<IAIConfiguredEmbeddingCapability>();
        if (embeddingCapability is null)
        {
            throw new InvalidOperationException(
                $"Provider '{profile.Model.ProviderId}' does not support embedding capability.");
        }

        return embeddingCapability;
    }
}
