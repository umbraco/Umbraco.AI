using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.RuntimeContext;

#pragma warning disable UMBRACOAI_EXTRACTION // IAIExtractionClient / IAIConfiguredExtractionCapability are experimental

namespace Umbraco.AI.Core.Extraction;

internal sealed class AIExtractionClientFactory : IAIExtractionClientFactory
{
    private readonly IAIConnectionService _connectionService;
    private readonly AIExtractionMiddlewareCollection _middleware;
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;
    private readonly IAIEditableModelResolver _modelResolver;

    public AIExtractionClientFactory(
        IAIConnectionService connectionService,
        AIExtractionMiddlewareCollection middleware,
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

    public async Task<IAIExtractionClient> CreateClientAsync(
        AIProfile profile,
        CancellationToken cancellationToken = default)
    {
        // Get configured provider with resolved settings
        var (extractionCapability, provider) = await GetConfiguredExtractionCapabilityAsync(profile, cancellationToken);

        // Resolve any provider-declared capability settings through the same editable-model pipeline
        // connections use ($-config resolution + validation + typing), so the provider receives a
        // strongly-typed, resolved object rather than a raw stored bag.
        var resolvedCapabilitySettings = _modelResolver.ResolveCapabilitySettings(
            provider,
            profile.Capability,
            profile.CapabilitySettings);

        // Create base client from provider with the profile's model and resolved capability settings.
        // Already wrapped in DeclaredSettingsExtractionClient (and CapabilitySettingsExtractionClient, when
        // the profile declares capability settings) by the capability base class itself.
        var client = await extractionCapability.CreateClientAsync(
            resolvedCapabilitySettings,
            profile.Model.ModelId,
            cancellationToken);

        // Wrap innermost so SDK exceptions are classified against the originating provider before
        // any middleware sees them.
        client = new AIErrorClassifyingExtractionClient(client, provider);

        // Apply middleware in order (tracking, telemetry, ...).
        client = ApplyMiddleware(client);

        // Wrap so middleware/tracking can access profile metadata in context.
        client = new ScopedProfileExtractionClient(
            client,
            profile,
            _runtimeContextAccessor,
            _scopeProvider,
            _contributors);

        // Outermost: a caller error (an invalid AIExtractionRequest — see ValidatingExtractionClient) must
        // be rejected as an ArgumentException before scope management, tracking, or error classification
        // run, and before the provider is ever touched. Do NOT move this inward to mirror
        // AISpeechToTextClientFactory's literal layering: wrapping it any further in would let a caller's
        // ArgumentException get caught and misreported as an AIProviderException by
        // AIErrorClassifyingExtractionClient.
        return new ValidatingExtractionClient(client);
    }

    private IAIExtractionClient ApplyMiddleware(IAIExtractionClient client)
    {
        // Apply middleware in collection order (controlled by AIExtractionMiddlewareCollectionBuilder)
        foreach (var middleware in _middleware)
        {
            client = middleware.Apply(client);
        }

        return client;
    }

    private async Task<(IAIConfiguredExtractionCapability Capability, IAIProvider Provider)> GetConfiguredExtractionCapabilityAsync(
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

        var extractionCapability = configured.GetCapability<IAIConfiguredExtractionCapability>();
        if (extractionCapability is null)
        {
            throw new InvalidOperationException(
                $"Provider '{profile.Model.ProviderId}' does not support extraction capability.");
        }

        return (extractionCapability, configured.Provider);
    }
}
