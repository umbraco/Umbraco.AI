using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Core.RuntimeContext;

namespace Umbraco.Ai.Core.Chat;

internal sealed class AiChatClientFactory : IAiChatClientFactory
{
    private readonly IAiConnectionService _connectionService;
    private readonly AiChatMiddlewareCollection _middleware;
    private readonly IAiRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAiRuntimeContextScopeProvider _scopeProvider;
    private readonly AiRuntimeContextContributorCollection _contributors;

    public AiChatClientFactory(
        IAiConnectionService connectionService,
        AiChatMiddlewareCollection middleware,
        IAiRuntimeContextAccessor runtimeContextAccessor,
        IAiRuntimeContextScopeProvider scopeProvider,
        AiRuntimeContextContributorCollection contributors)
    {
        _connectionService = connectionService;
        _middleware = middleware;
        _runtimeContextAccessor = runtimeContextAccessor;
        _scopeProvider = scopeProvider;
        _contributors = contributors;
    }

    public async Task<IChatClient> CreateClientAsync(AiProfile profile, CancellationToken cancellationToken = default)
    {
        // Get configured provider with resolved settings
        var chatCapability = await GetConfiguredChatCapabilityAsync(profile, cancellationToken);

        // Create base client from provider with the profile's model
        var chatClient = chatCapability.CreateClient(profile.Model.ModelId);

        // Apply middleware in order
        chatClient = ApplyMiddleware(chatClient);

        // Wrap in scoped client to set profile metadata per-execution
        // This is the outermost wrapper so middleware can access profile metadata in context
        // Creates scope if needed for standalone usage
        chatClient = new ScopedProfileChatClient(
            chatClient,
            profile,
            _runtimeContextAccessor,
            _scopeProvider,
            _contributors);

        return chatClient;
    }

    private IChatClient ApplyMiddleware(IChatClient client)
    {
        // Apply middleware in collection order (controlled by AiChatMiddlewareCollectionBuilder)
        // Function invocation is now a middleware (AiFunctionInvokingChatMiddleware) and can be
        // ordered relative to other middleware using InsertBefore/InsertAfter
        foreach (var middleware in _middleware)
        {
            client = middleware.Apply(client);
        }

        return client;
    }

    private async Task<IAiConfiguredChatCapability> GetConfiguredChatCapabilityAsync(
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

        var chatCapability = configured.GetCapability<IAiConfiguredChatCapability>();
        if (chatCapability is null)
        {
            throw new InvalidOperationException(
                $"Provider '{profile.Model.ProviderId}' does not support chat capability.");
        }

        return chatCapability;
    }
}
