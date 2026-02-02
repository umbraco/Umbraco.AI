using Umbraco.AI.Core.Contexts.Resolvers;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Default implementation of <see cref="IAiContextResolutionService"/> that aggregates
/// context from all registered resolvers.
/// </summary>
internal sealed class AIContextResolutionService : IAiContextResolutionService
{
    private readonly AIContextResolverCollection _resolvers;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIContextResolutionService"/> class.
    /// </summary>
    /// <param name="resolvers">The collection of registered context resolvers.</param>
    public AIContextResolutionService(AIContextResolverCollection resolvers)
    {
        _resolvers = resolvers;
    }

    /// <inheritdoc />
    public async Task<AIResolvedContext> ResolveContextAsync(CancellationToken cancellationToken = default)
    {
        var allSources = new List<AIContextSource>();
        var allResources = new List<AIResolvedResource>();
        var seenResourceIds = new HashSet<Guid>();

        // Execute each resolver in order
        foreach (var resolver in _resolvers)
        {
            var result = await resolver.ResolveAsync(cancellationToken);
            var resolverTypeName = resolver.GetType().Name;

            // Add sources
            foreach (var source in result.Sources)
            {
                allSources.Add(new AIContextSource(resolverTypeName, source.EntityName, source.ContextName));
            }

            // Process resources
            foreach (var resource in result.Resources)
            {
                // Later resolvers override earlier ones (deduplication)
                if (seenResourceIds.Contains(resource.Id))
                {
                    allResources.RemoveAll(r => r.Id == resource.Id);
                }

                seenResourceIds.Add(resource.Id);

                // Convert to AIResolvedResource with SourceLevel set
                allResources.Add(new AIResolvedResource
                {
                    Id = resource.Id,
                    ResourceTypeId = resource.ResourceTypeId,
                    Name = resource.Name,
                    Description = resource.Description,
                    Data = resource.Data,
                    InjectionMode = resource.InjectionMode,
                    Source = resolverTypeName,
                    ContextName = resource.ContextName
                });
            }
        }

        return new AIResolvedContext
        {
            Sources = allSources,
            AllResources = allResources,
            InjectedResources = allResources
                .Where(r => r.InjectionMode == AIContextResourceInjectionMode.Always)
                .ToList(),
            OnDemandResources = allResources
                .Where(r => r.InjectionMode == AIContextResourceInjectionMode.OnDemand)
                .ToList()
        };
    }
}
