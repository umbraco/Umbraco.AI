using Umbraco.Ai.Core.Profiles;

namespace Umbraco.Ai.Core.Context;

/// <summary>
/// Default implementation of <see cref="IAiContextResolver"/>.
/// </summary>
internal sealed class AiContextResolver : IAiContextResolver
{
    private readonly IAiContextRepository _contextRepository;
    private readonly IAiProfileService _profileService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiContextResolver"/> class.
    /// </summary>
    /// <param name="contextRepository">The context repository.</param>
    /// <param name="profileService">The profile service.</param>
    public AiContextResolver(
        IAiContextRepository contextRepository,
        IAiProfileService profileService)
    {
        _contextRepository = contextRepository;
        _profileService = profileService;
    }

    /// <inheritdoc />
    public async Task<AiResolvedContext> ResolveAsync(
        AiContextResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        var sources = new List<AiContextSource>();
        var allResources = new List<AiResolvedResource>();
        var seenResourceIds = new HashSet<Guid>();

        // Resolution order: Profile → Agent → Prompt → Content
        // Later levels can override earlier levels by having same resource ID

        // 1. Profile-level context
        if (request.ProfileId.HasValue)
        {
            var profile = await _profileService.GetProfileAsync(request.ProfileId.Value, cancellationToken);
            if (profile?.ContextIds is { Count: > 0 })
            {
                await ResolveContextIdsAsync(
                    profile.ContextIds,
                    "Profile",
                    profile.Name,
                    sources,
                    allResources,
                    seenResourceIds,
                    cancellationToken);
            }
        }

        // 2. Agent-level context (from Umbraco.Ai.Agent)
        if (request.AgentContextIds is not null)
        {
            await ResolveContextIdsAsync(
                request.AgentContextIds,
                "Agent",
                request.AgentName,
                sources,
                allResources,
                seenResourceIds,
                cancellationToken);
        }

        // 3. Prompt-level context (from Umbraco.Ai.Prompt)
        if (request.PromptContextIds is not null)
        {
            await ResolveContextIdsAsync(
                request.PromptContextIds,
                "Prompt",
                request.PromptName,
                sources,
                allResources,
                seenResourceIds,
                cancellationToken);
        }

        // 4. Content-level context (via property editor - V2)
        // TODO: Implement content tree walking for inherited context

        return new AiResolvedContext
        {
            Sources = sources,
            AllResources = allResources,
            InjectedResources = allResources
                .Where(r => r.InjectionMode == AiContextResourceInjectionMode.Always)
                .ToList(),
            OnDemandResources = allResources
                .Where(r => r.InjectionMode == AiContextResourceInjectionMode.OnDemand)
                .ToList()
        };
    }

    /// <inheritdoc />
    public Task<AiResolvedContext> ResolveForProfileAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        return ResolveAsync(
            new AiContextResolutionRequest { ProfileId = profileId },
            cancellationToken);
    }

    private async Task ResolveContextIdsAsync(
        IEnumerable<Guid> contextIds,
        string level,
        string? entityName,
        List<AiContextSource> sources,
        List<AiResolvedResource> allResources,
        HashSet<Guid> seenResourceIds,
        CancellationToken cancellationToken)
    {
        foreach (var contextId in contextIds)
        {
            var context = await _contextRepository.GetByIdAsync(contextId, cancellationToken);
            if (context is null)
                continue;

            sources.Add(new AiContextSource(level, entityName, context.Name));

            foreach (var resource in context.Resources.OrderBy(r => r.SortOrder))
            {
                // If we've seen this resource ID before, remove the old one (later levels override)
                if (seenResourceIds.Contains(resource.Id))
                {
                    allResources.RemoveAll(r => r.Id == resource.Id);
                }

                seenResourceIds.Add(resource.Id);
                allResources.Add(new AiResolvedResource
                {
                    Id = resource.Id,
                    ResourceTypeId = resource.ResourceTypeId,
                    Name = resource.Name,
                    Description = resource.Description,
                    Data = resource.Data,
                    InjectionMode = resource.InjectionMode,
                    SourceLevel = level,
                    ContextName = context.Name
                });
            }
        }
    }
}
