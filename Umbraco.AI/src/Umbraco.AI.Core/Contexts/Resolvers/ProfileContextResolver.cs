using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Contexts.Resolvers;

/// <summary>
/// Resolves context from profile-level context assignments.
/// </summary>
/// <remarks>
/// This resolver reads the profile ID from <see cref="Constants.ContextKeys.ProfileId"/> in the request properties,
/// then resolves any context IDs configured on the profile's chat settings. It also honors
/// <see cref="Constants.ContextKeys.ContextIdsOverride"/> set by the inline chat builder (via
/// <see cref="InlineChat.AIChatBuilder.WithContexts(Guid[])"/>), using the override list in place of
/// the profile's configured context IDs.
/// </remarks>
internal sealed class ProfileContextResolver : IAIContextResolver
{
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAIContextService _contextService;
    private readonly IAIProfileService _profileService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileContextResolver"/> class.
    /// </summary>
    /// <param name="runtimeContextAccessor">The runtime context accessor.</param>
    /// <param name="contextService">The context service.</param>
    /// <param name="profileService">The profile service.</param>
    public ProfileContextResolver(
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IAIContextService contextService,
        IAIProfileService profileService)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _contextService = contextService;
        _profileService = profileService;
    }

    /// <inheritdoc />
    public async Task<AIContextResolverResult> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var contextIdsOverride = _runtimeContextAccessor.Context?.GetValue<IReadOnlyList<Guid>>(Constants.ContextKeys.ContextIdsOverride);
        var additionalContextIds = _runtimeContextAccessor.Context?.GetValue<IReadOnlyList<Guid>>(Constants.ContextKeys.AdditionalContextIds);

        // Short-circuit: when override is set we don't need the profile's configured contexts,
        // and when neither override nor additional is set we need a profile to source any contexts.
        var profileId = _runtimeContextAccessor.Context?.GetValue<Guid>(Constants.ContextKeys.ProfileId);
        if (!profileId.HasValue)
        {
            return AIContextResolverResult.Empty;
        }

        IReadOnlyList<Guid> baseIds;
        string? entityName;

        if (contextIdsOverride is not null)
        {
            // Override replaces the profile's configured contexts; skip fetching the profile for its ContextIds.
            baseIds = contextIdsOverride;
            entityName = null;
        }
        else
        {
            var profile = await _profileService.GetProfileAsync(profileId.Value, cancellationToken);
            baseIds = profile?.Settings is AIChatProfileSettings chatSettings ? chatSettings.ContextIds : [];
            entityName = profile?.Name;
        }

        var combined = Combine(baseIds, additionalContextIds);
        if (combined.Count == 0)
        {
            return AIContextResolverResult.Empty;
        }

        return await ResolveContextIdsAsync(combined, entityName, cancellationToken);
    }

    private static IReadOnlyList<Guid> Combine(IReadOnlyList<Guid> primary, IReadOnlyList<Guid>? additional)
    {
        if (additional is null || additional.Count == 0)
        {
            return primary;
        }

        var combined = new List<Guid>(primary.Count + additional.Count);
        combined.AddRange(primary);
        foreach (var id in additional)
        {
            if (!combined.Contains(id))
            {
                combined.Add(id);
            }
        }

        return combined;
    }

    private async Task<AIContextResolverResult> ResolveContextIdsAsync(
        IEnumerable<Guid> contextIds,
        string? entityName,
        CancellationToken cancellationToken)
    {
        var resources = new List<AIContextResolverResource>();
        var sources = new List<AIContextResolverSource>();

        foreach (var contextId in contextIds)
        {
            var context = await _contextService.GetContextAsync(contextId, cancellationToken);
            if (context is null)
            {
                continue;
            }

            sources.Add(new AIContextResolverSource(entityName, context.Name));

            foreach (var resource in context.Resources.OrderBy(r => r.SortOrder))
            {
                resources.Add(new AIContextResolverResource
                {
                    Id = resource.Id,
                    ResourceTypeId = resource.ResourceTypeId,
                    Name = resource.Name,
                    Description = resource.Description,
                    Settings = resource.Settings,
                    InjectionMode = resource.InjectionMode,
                    ContextName = context.Name
                });
            }
        }

        return new AIContextResolverResult
        {
            Resources = resources,
            Sources = sources
        };
    }
}
