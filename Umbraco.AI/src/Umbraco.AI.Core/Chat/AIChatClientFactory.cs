using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Chat;

internal sealed class AIChatClientFactory : IAIChatClientFactory
{
    private readonly IAIConnectionService _connectionService;
    private readonly AIChatMiddlewareCollection _middleware;
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;
    private readonly IAIEditableModelResolver _modelResolver;

    public AIChatClientFactory(
        IAIConnectionService connectionService,
        AIChatMiddlewareCollection middleware,
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IAIRuntimeContextScopeProvider scopeProvider,
        AIRuntimeContextContributorCollection contributors,
        IAIEditableModelResolver modelResolver)
    {
        _connectionService = connectionService;
        _middleware = middleware;
        _runtimeContextAccessor = runtimeContextAccessor;
        _scopeProvider = scopeProvider;
        _contributors = contributors;
        _modelResolver = modelResolver;
    }

    public async Task<IChatClient> CreateClientAsync(AIProfile profile, CancellationToken cancellationToken = default)
    {
        // Get configured provider with resolved settings
        var (chatCapability, provider) = await GetConfiguredChatCapabilityAsync(profile, cancellationToken);

        // Resolve the provider-declared profile settings (e.g. reasoning effort) through the same
        // editable-model pipeline connections use ($-config resolution + validation + typing), so the
        // provider receives a strongly-typed, resolved object rather than a raw stored bag.
        var resolvedCapabilitySettings = _modelResolver.ResolveCapabilitySettings(
            provider,
            profile.Capability,
            profile.CapabilitySettings);

        // Create base client from provider with the profile's model and resolved profile settings
        var chatClient = await chatCapability.CreateClientAsync(
            resolvedCapabilitySettings,
            profile.Model.ModelId,
            cancellationToken);

        // Wrap innermost so SDK exceptions are classified against the originating provider before
        // any middleware sees them (and every model round-trip inside function-invoking middleware
        // passes back through here).
        chatClient = new AIErrorClassifyingChatClient(chatClient, provider);

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
        // Apply middleware in collection order (controlled by AIChatMiddlewareCollectionBuilder)
        // Function invocation is now a middleware (AIFunctionInvokingChatMiddleware) and can be
        // ordered relative to other middleware using InsertBefore/InsertAfter
        foreach (var middleware in _middleware)
        {
            client = middleware.Apply(client);
        }

        return client;
    }

    private async Task<(IAIConfiguredChatCapability Capability, IAIProvider Provider)> GetConfiguredChatCapabilityAsync(
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

        var chatCapability = configured.GetCapability<IAIConfiguredChatCapability>();
        if (chatCapability is null)
        {
            throw new InvalidOperationException(
                $"Provider '{profile.Model.ProviderId}' does not support chat capability.");
        }

        return (chatCapability, configured.Provider);
    }
}
