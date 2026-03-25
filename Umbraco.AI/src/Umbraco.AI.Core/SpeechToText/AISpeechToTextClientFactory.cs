using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;

#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

namespace Umbraco.AI.Core.SpeechToText;

internal sealed class AISpeechToTextClientFactory : IAISpeechToTextClientFactory
{
    private readonly IAIConnectionService _connectionService;
    private readonly AISpeechToTextMiddlewareCollection _middleware;

    public AISpeechToTextClientFactory(
        IAIConnectionService connectionService,
        AISpeechToTextMiddlewareCollection middleware)
    {
        _connectionService = connectionService;
        _middleware = middleware;
    }

    public async Task<ISpeechToTextClient> CreateClientAsync(
        AIProfile profile,
        CancellationToken cancellationToken = default)
    {
        // Get configured provider with resolved settings
        var speechToTextCapability = await GetConfiguredSpeechToTextCapabilityAsync(profile, cancellationToken);

        // Create base client from provider with the profile's model
        var client = await speechToTextCapability.CreateClientAsync(profile.Model.ModelId, cancellationToken);

        // Apply middleware in order
        client = ApplyMiddleware(client);

        return client;
    }

    private ISpeechToTextClient ApplyMiddleware(ISpeechToTextClient client)
    {
        // Apply middleware in collection order (controlled by AISpeechToTextMiddlewareCollectionBuilder)
        foreach (var middleware in _middleware)
        {
            client = middleware.Apply(client);
        }

        return client;
    }

    private async Task<IAIConfiguredSpeechToTextCapability> GetConfiguredSpeechToTextCapabilityAsync(
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

        var speechToTextCapability = configured.GetCapability<IAIConfiguredSpeechToTextCapability>();
        if (speechToTextCapability is null)
        {
            throw new InvalidOperationException(
                $"Provider '{profile.Model.ProviderId}' does not support speech-to-text capability.");
        }

        return speechToTextCapability;
    }
}
