using Umbraco.Ai.Core.Contexts.Resolvers;
using Umbraco.Ai.Core.RuntimeContext;

namespace Umbraco.Ai.Core.Contexts;

/// <summary>
/// Default implementation of <see cref="IAiContextResolutionService"/> that aggregates
/// context from all registered resolvers.
/// </summary>
internal sealed class AiContextResolutionService : IAiContextResolutionService
{
    private readonly AiContextResolverCollection _resolvers;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiContextResolutionService"/> class.
    /// </summary>
    /// <param name="resolvers">The collection of registered context resolvers.</param>
    public AiContextResolutionService(AiContextResolverCollection resolvers)
    {
        _resolvers = resolvers;
    }

    /// <inheritdoc />
    public async Task<AiResolvedContext> ResolveContextAsync(CancellationToken cancellationToken = default)
    {
        var allSources = new List<AiContextSource>();
        var allResources = new List<AiResolvedResource>();
        var seenResourceIds = new HashSet<Guid>();

        // Execute each resolver in order
        foreach (var resolver in _resolvers)
        {
            var result = await resolver.ResolveAsync(cancellationToken);
            var resolverTypeName = resolver.GetType().Name;

            // Add sources
            foreach (var source in result.Sources)
            {
                allSources.Add(new AiContextSource(resolverTypeName, source.EntityName, source.ContextName));
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

                // Convert to AiResolvedResource with SourceLevel set
                allResources.Add(new AiResolvedResource
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

        return new AiResolvedContext
        {
            Sources = allSources,
            AllResources = allResources,
            InjectedResources = allResources
                .Where(r => r.InjectionMode == AiContextResourceInjectionMode.Always)
                .ToList(),
            OnDemandResources = allResources
                .Where(r => r.InjectionMode == AiContextResourceInjectionMode.OnDemand)
                .ToList()
        };
    }
}
