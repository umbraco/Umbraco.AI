using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Core.Settings;

namespace Umbraco.Ai.Core.Embeddings;

internal sealed class AiEmbeddingGeneratorFactory : IAiEmbeddingGeneratorFactory
{
    private readonly AiProviderCollection _providers;
    private readonly IAiConnectionService _connectionService;
    private readonly IAiSettingsResolver _settingsResolver;
    private readonly AiEmbeddingMiddlewareCollection _middleware;

    public AiEmbeddingGeneratorFactory(
        AiProviderCollection providers,
        IAiConnectionService connectionService,
        IAiSettingsResolver settingsResolver,
        AiEmbeddingMiddlewareCollection middleware)
    {
        _providers = providers;
        _connectionService = connectionService;
        _settingsResolver = settingsResolver;
        _middleware = middleware;
    }

    public async Task<IEmbeddingGenerator<string, Embedding<float>>> CreateGeneratorAsync(
        AiProfile profile,
        CancellationToken cancellationToken = default)
    {
        // Resolve connection settings
        var connectionSettings = await ResolveConnectionSettingsAsync(profile, cancellationToken);

        // Get embedding capability from provider
        var embeddingCapability = _providers.GetCapability<IAiEmbeddingCapability>(profile.Model.ProviderId);
        if (embeddingCapability == null)
        {
            throw new InvalidOperationException(
                $"Provider '{profile.Model.ProviderId}' does not support embedding capability.");
        }

        // Create base generator from provider
        var generator = embeddingCapability.CreateGenerator(connectionSettings);

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

    private async Task<object?> ResolveConnectionSettingsAsync(
        AiProfile profile,
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

        // Validate connection provider matches profile's model provider
        if (!string.Equals(connection.ProviderId, profile.Model.ProviderId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Connection '{connection.Name}' is for provider '{connection.ProviderId}' " +
                $"but profile '{profile.Name}' requires provider '{profile.Model.ProviderId}'.");
        }

        // Get provider and resolve settings to typed format
        var provider = _providers.GetById(connection.ProviderId);
        if (provider is null)
        {
            throw new InvalidOperationException(
                $"Provider '{connection.ProviderId}' not found.");
        }

        // Resolve settings (handles JsonElement deserialization, env vars, validation)
        var resolvedSettings = _settingsResolver.ResolveSettingsForProvider(provider, connection.Settings);
        return resolvedSettings;
    }
}
