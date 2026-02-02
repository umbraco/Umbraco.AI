using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Contexts.Resolvers;

/// <summary>
/// Resolves context from profile-level context assignments.
/// </summary>
/// <remarks>
/// This resolver reads the profile ID from <see cref="Constants.ContextKeys.ProfileId"/> in the request properties,
/// then resolves any context IDs configured on the profile's chat settings.
/// </remarks>
internal sealed class ProfileContextResolver : IAiContextResolver
{
    private readonly IAiRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAiContextService _contextService;
    private readonly IAiProfileService _profileService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileContextResolver"/> class.
    /// </summary>
    /// <param name="runtimeContextAccessor">The runtime context accessor.</param>
    /// <param name="contextService">The context service.</param>
    /// <param name="profileService">The profile service.</param>
    public ProfileContextResolver(
        IAiRuntimeContextAccessor runtimeContextAccessor,
        IAiContextService contextService,
        IAiProfileService profileService)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _contextService = contextService;
        _profileService = profileService;
    }

    /// <inheritdoc />
    public async Task<AIContextResolverResult> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var profileId = _runtimeContextAccessor.Context?.GetValue<Guid>(Constants.ContextKeys.ProfileId);
        if (!profileId.HasValue)
        {
            return AIContextResolverResult.Empty;
        }

        var profile = await _profileService.GetProfileAsync(profileId.Value, cancellationToken);
        if (profile?.Settings is not AIChatProfileSettings chatSettings || chatSettings.ContextIds.Count == 0)
        {
            return AIContextResolverResult.Empty;
        }

        return await ResolveContextIdsAsync(chatSettings.ContextIds, profile.Name, cancellationToken);
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
                    Data = resource.Data,
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
