using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Middleware;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Core.Registry;
using Umbraco.Ai.Core.Settings;

namespace Umbraco.Ai.Core.Factories;

internal sealed class AiEmbeddingGeneratorFactory : IAiEmbeddingGeneratorFactory
{
    private readonly IAiRegistry _registry;
    private readonly IAiConnectionService _connectionService;
    private readonly IAiSettingsResolver _settingsResolver;
    private readonly IEnumerable<IAiEmbeddingMiddleware> _middleware;

    public AiEmbeddingGeneratorFactory(
        IAiRegistry registry,
        IAiConnectionService connectionService,
        IAiSettingsResolver settingsResolver,
        IEnumerable<IAiEmbeddingMiddleware> middleware)
    {
        _registry = registry;
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

        // Get embedding capability from registry
        var embeddingCapability = _registry.GetCapability<IAiEmbeddingCapability>(profile.Model.ProviderId);
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
        // Apply middleware in order (lowest Order value first)
        foreach (var middleware in _middleware.OrderBy(m => m.Order))
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
        var provider = _registry.GetProvider(connection.ProviderId);
        if (provider is null)
        {
            throw new InvalidOperationException(
                $"Provider '{connection.ProviderId}' not found in registry.");
        }

        // Resolve settings (handles JsonElement deserialization, env vars, validation)
        var resolvedSettings = _settingsResolver.ResolveSettingsForProvider(provider, connection.Settings);
        return resolvedSettings;
    }
}
