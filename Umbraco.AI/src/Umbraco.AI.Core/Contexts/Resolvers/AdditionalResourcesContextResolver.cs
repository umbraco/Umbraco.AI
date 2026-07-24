using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Contexts.Resolvers;

/// <summary>
/// Resolves ad-hoc resources supplied directly by the caller via
/// <see cref="Constants.ContextKeys.AdditionalResources"/>, rather than sourced from a persisted
/// <see cref="Models.AIContext"/>. Used by callers that own resources directly — e.g. a Copilot
/// Workspace project's attached resources — so they participate in the normal resolve→format→inject
/// pipeline alongside context-sourced resources.
/// </summary>
/// <remarks>
/// A no-op for every existing caller: when the runtime-context key is absent (the default) this
/// resolver returns <see cref="AIContextResolverResult.Empty"/>. Registered last so caller-owned
/// resources take precedence over context-sourced ones on an id collision.
/// </remarks>
internal sealed class AdditionalResourcesContextResolver : IAIContextResolver
{
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdditionalResourcesContextResolver"/> class.
    /// </summary>
    /// <param name="runtimeContextAccessor">The runtime context accessor.</param>
    public AdditionalResourcesContextResolver(IAIRuntimeContextAccessor runtimeContextAccessor)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
    }

    /// <inheritdoc />
    public Task<AIContextResolverResult> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var resources = _runtimeContextAccessor.Context?
            .GetValue<IReadOnlyList<AIContextResolverResource>>(Constants.ContextKeys.AdditionalResources);

        if (resources is null || resources.Count == 0)
        {
            return Task.FromResult(AIContextResolverResult.Empty);
        }

        return Task.FromResult(new AIContextResolverResult { Resources = resources });
    }
}
