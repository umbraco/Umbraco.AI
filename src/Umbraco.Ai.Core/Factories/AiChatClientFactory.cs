using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Middleware;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Core.Registry;
using Umbraco.Ai.Core.Settings;

namespace Umbraco.Ai.Core.Factories;

internal sealed class AiChatClientFactory : IAiChatClientFactory
{
    private readonly IAiRegistry _registry;
    private readonly IAiConnectionService _connectionService;
    private readonly IAiSettingsResolver _settingsResolver;
    private readonly AiChatMiddlewareCollection _middleware;

    public AiChatClientFactory(
        IAiRegistry registry,
        IAiConnectionService connectionService,
        IAiSettingsResolver settingsResolver,
        AiChatMiddlewareCollection middleware)
    {
        _registry = registry;
        _connectionService = connectionService;
        _settingsResolver = settingsResolver;
        _middleware = middleware;
    }

    public async Task<IChatClient> CreateClientAsync(AiProfile profile, CancellationToken cancellationToken = default)
    {
        // Resolve connection settings
        var connectionSettings = await ResolveConnectionSettingsAsync(profile, cancellationToken);

        // Get chat capability from registry
        var chatCapability = _registry.GetCapability<IAiChatCapability>(profile.Model.ProviderId);
        if (chatCapability == null)
        {
            throw new InvalidOperationException(
                $"Provider '{profile.Model.ProviderId}' does not support chat capability.");
        }

        // Create base client from provider
        var chatClient = chatCapability.CreateClient(connectionSettings);

        // Apply middleware in order
        chatClient = ApplyMiddleware(chatClient);

        return chatClient;
    }

    private IChatClient ApplyMiddleware(IChatClient client)
    {
        // Apply middleware in collection order (controlled by AiChatMiddlewareCollectionBuilder)
        foreach (var middleware in _middleware)
        {
            client = middleware.Apply(client);
        }

        return client;
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
