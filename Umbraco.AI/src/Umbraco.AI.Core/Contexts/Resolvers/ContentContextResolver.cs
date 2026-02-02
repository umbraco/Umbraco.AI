using Umbraco.AI.Core.RuntimeContext;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.Contexts.Resolvers;

/// <summary>
/// Resolves context from content nodes by finding the nearest context picker property value.
/// </summary>
/// <remarks>
/// <para>
/// This resolver reads the content ID from <see cref="IAIRuntimeContextAccessor"/>, preferring
/// <see cref="AIRuntimeContextKeys.ParentEntityId"/> (for new entities) over
/// <see cref="AIRuntimeContextKeys.EntityId"/>. It then walks up the content tree
/// (current node + ancestors) to find the nearest property using the AI Context
/// Picker editor (<c>Uai.ContextPicker</c>).
/// </para>
/// <para>
/// The first non-empty context picker value found while walking up the tree is used.
/// This allows content to inherit context from parent/ancestor nodes.
/// </para>
/// </remarks>
internal sealed class ContentContextResolver : IAIContextResolver
{
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAIContextService _contextService;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentContextResolver"/> class.
    /// </summary>
    /// <param name="runtimeContextAccessor">The runtime context accessor.</param>
    /// <param name="contextService">The context service.</param>
    /// <param name="umbracoContextAccessor">The Umbraco context accessor.</param>
    public ContentContextResolver(
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IAIContextService contextService,
        IUmbracoContextAccessor umbracoContextAccessor)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _contextService = contextService;
        _umbracoContextAccessor = umbracoContextAccessor;
    }

    /// <inheritdoc />
    public async Task<AIContextResolverResult> ResolveAsync(CancellationToken cancellationToken = default)
    {
        // Get content ID from RuntimeContext (set by orchestrators like AGUIStreamingService)
        var contentId = _runtimeContextAccessor.Context?.GetValue<Guid>(Constants.ContextKeys.ParentEntityId)
            ?? _runtimeContextAccessor.Context?.GetValue<Guid>(Constants.ContextKeys.EntityId);
        if (!contentId.HasValue)
        {
            return AIContextResolverResult.Empty;
        }

        // Try to get the Umbraco context
        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
        {
            return AIContextResolverResult.Empty;
        }

        // Get the published content
        var content = umbracoContext.Content?.GetById(contentId.Value);
        if (content is null)
        {
            return AIContextResolverResult.Empty;
        }

        // Walk up the tree to find the nearest context picker property with a value
        var (contexts, source) = FindNearestContexts(content);
        if (contexts is null || contexts.Count == 0)
        {
            return AIContextResolverResult.Empty;
        }
        
        // Use the source content's key and name for tracking
        var contentKey = source?.Key ?? content.Key;
        var contentName = source?.Name ?? content.Name;

        return await ResolveContextsAsync(contexts, contentKey, contentName, cancellationToken);
    }

    /// <summary>
    /// Walks up the content tree to find the nearest context picker property with a value.
    /// </summary>
    private static (IReadOnlyCollection<AIContext>? Contexts, IPublishedContent? Source) FindNearestContexts(IPublishedContent content)
    {
        // Check current node and ancestors (from closest to root)
        IPublishedContent? node = content;
        
        while (node is not null)
        {
            // Find any property using the context picker editor
            foreach (var property in node.Properties)
            {
                if (property.PropertyType.EditorAlias != Constants.PropertyEditors.Aliases.ContextPicker)
                {
                    continue;
                }

                // Get the property value - could be single AIContext or IEnumerable<AIContext>
                var value = property.GetValue();
                var contexts = ExtractContexts(value)?.ToList();

                if (contexts is not null && contexts.Count > 0)
                {
                    return (contexts, node);
                }
            }

            // Move to parent
            node = node.Parent();
        }

        return (null, null);
    }

    /// <summary>
    /// Extracts AIContext instances from a property value.
    /// </summary>
    private static IEnumerable<AIContext>? ExtractContexts(object? value)
    {
        return value switch
        {
            IEnumerable<AIContext> contexts => contexts,
            AIContext context => [context],
            _ => null
        };
    }

    private async Task<AIContextResolverResult> ResolveContextsAsync(
        IEnumerable<AIContext> contexts,
        Guid? contentKey,
        string? contentName,
        CancellationToken cancellationToken)
    {
        var resources = new List<AIContextResolverResource>();
        var sources = new List<AIContextResolverSource>();

        // Use content name and key for tracking
        var entityName = $"{contentName ?? "Unknown"} ({contentKey ?? Guid.Empty})";

        foreach (var context in contexts)
        {
            // Re-fetch the context to ensure we have fresh data
            var freshContext = await _contextService.GetContextAsync(context.Id, cancellationToken);
            if (freshContext is null)
            {
                continue;
            }

            sources.Add(new AIContextResolverSource(entityName, freshContext.Name));

            foreach (var resource in freshContext.Resources.OrderBy(r => r.SortOrder))
            {
                resources.Add(new AIContextResolverResource
                {
                    Id = resource.Id,
                    ResourceTypeId = resource.ResourceTypeId,
                    Name = resource.Name,
                    Description = resource.Description,
                    Data = resource.Data,
                    InjectionMode = resource.InjectionMode,
                    ContextName = freshContext.Name
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
