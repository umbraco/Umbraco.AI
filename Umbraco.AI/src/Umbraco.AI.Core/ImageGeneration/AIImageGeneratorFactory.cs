using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.RuntimeContext;

#pragma warning disable MEAI001 // IImageGenerator is experimental in M.E.AI
#pragma warning disable UMBRACOAI_IMAGEGEN // Internal plumbing for the experimental image-generation API

namespace Umbraco.AI.Core.ImageGeneration;

internal sealed class AIImageGeneratorFactory : IAIImageGeneratorFactory
{
    private readonly IAIConnectionService _connectionService;
    private readonly AIImageGenerationMiddlewareCollection _middleware;
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;

    public AIImageGeneratorFactory(
        IAIConnectionService connectionService,
        AIImageGenerationMiddlewareCollection middleware,
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IAIRuntimeContextScopeProvider scopeProvider,
        AIRuntimeContextContributorCollection contributors)
    {
        _connectionService = connectionService;
        _middleware = middleware;
        _runtimeContextAccessor = runtimeContextAccessor;
        _scopeProvider = scopeProvider;
        _contributors = contributors;
    }

    public async Task<IImageGenerator> CreateGeneratorAsync(
        AIProfile profile,
        CancellationToken cancellationToken = default)
    {
        // Get configured provider with resolved settings
        var (imageGeneratorCapability, provider) = await GetConfiguredImageGeneratorCapabilityAsync(profile, cancellationToken);

        // Create base generator from provider with the profile's model
        var generator = await imageGeneratorCapability.CreateGeneratorAsync(profile.Model.ModelId, cancellationToken);

        // Wrap innermost so SDK exceptions are classified against the originating provider before
        // any middleware sees them.
        generator = new AIErrorClassifyingImageGenerator(generator, provider);

        // Apply middleware in order
        generator = ApplyMiddleware(generator);

        // Wrap in scoped generator to set profile metadata per-execution.
        // This is the outermost wrapper so middleware can access profile metadata in context.
        // Creates a scope if needed for standalone usage.
        generator = new ScopedProfileImageGenerator(
            generator,
            profile,
            _runtimeContextAccessor,
            _scopeProvider,
            _contributors);

        return generator;
    }

    private IImageGenerator ApplyMiddleware(IImageGenerator generator)
    {
        // Apply middleware in collection order (controlled by AIImageGenerationMiddlewareCollectionBuilder)
        foreach (var middleware in _middleware)
        {
            generator = middleware.Apply(generator);
        }

        return generator;
    }

    private async Task<(IAIConfiguredImageGeneratorCapability Capability, IAIProvider Provider)> GetConfiguredImageGeneratorCapabilityAsync(
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

        var imageGeneratorCapability = configured.GetCapability<IAIConfiguredImageGeneratorCapability>();
        if (imageGeneratorCapability is null)
        {
            throw new InvalidOperationException(
                $"Provider '{profile.Model.ProviderId}' does not support image-generation capability.");
        }

        return (imageGeneratorCapability, configured.Provider);
    }
}
