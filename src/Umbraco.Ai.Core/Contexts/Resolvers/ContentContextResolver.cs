using Umbraco.Ai.Core.RequestContext;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Umbraco.Ai.Core.Contexts.Resolvers;

/// <summary>
/// Resolves context from content nodes by finding the nearest context picker property value.
/// </summary>
/// <remarks>
/// <para>
/// This resolver reads the content ID from <see cref="AiRequestContextKeys.ContentId"/> in the request properties,
/// then walks up the content tree (current node + ancestors) to find the nearest property
/// using the AI Context Picker editor (<c>Uai.ContextPicker</c>).
/// </para>
/// <para>
/// The first non-empty context picker value found while walking up the tree is used.
/// This allows content to inherit context from parent/ancestor nodes.
/// </para>
/// </remarks>
internal sealed class ContentContextResolver : IAiContextResolver
{

    private readonly IAiContextService _contextService;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentContextResolver"/> class.
    /// </summary>
    /// <param name="contextService">The context service.</param>
    /// <param name="umbracoContextAccessor">The Umbraco context accessor.</param>
    public ContentContextResolver(
        IAiContextService contextService,
        IUmbracoContextAccessor umbracoContextAccessor)
    {
        _contextService = contextService;
        _umbracoContextAccessor = umbracoContextAccessor;
    }

    /// <inheritdoc />
    public async Task<AiContextResolverResult> ResolveAsync(
        AiContextResolverRequest request,
        CancellationToken cancellationToken = default)
    {
        var contentId = request.GetGuidProperty(AiRequestContextKeys.ContentId);
        if (!contentId.HasValue)
        {
            return AiContextResolverResult.Empty;
        }

        // Try to get the Umbraco context
        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
        {
            return AiContextResolverResult.Empty;
        }

        // Get the published content
        var content = umbracoContext.Content?.GetById(contentId.Value);
        if (content is null)
        {
            return AiContextResolverResult.Empty;
        }

        // Walk up the tree to find the nearest context picker property with a value
        var (contexts, source) = FindNearestContexts(content);
        if (contexts is null || contexts.Count == 0)
        {
            return AiContextResolverResult.Empty;
        }
        
        // Use the source content's key and name for tracking
        var contentKey = source?.Key ?? content.Key;
        var contentName = source?.Name ?? content.Name;

        return await ResolveContextsAsync(contexts, contentKey, contentName, cancellationToken);
    }

    /// <summary>
    /// Walks up the content tree to find the nearest context picker property with a value.
    /// </summary>
    private static (IReadOnlyCollection<AiContext>? Contexts, IPublishedContent? Source) FindNearestContexts(IPublishedContent content)
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

                // Get the property value - could be single AiContext or IEnumerable<AiContext>
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
    /// Extracts AiContext instances from a property value.
    /// </summary>
    private static IEnumerable<AiContext>? ExtractContexts(object? value)
    {
        return value switch
        {
            IEnumerable<AiContext> contexts => contexts,
            AiContext context => [context],
            _ => null
        };
    }

    private async Task<AiContextResolverResult> ResolveContextsAsync(
        IEnumerable<AiContext> contexts,
        Guid? contentKey,
        string? contentName,
        CancellationToken cancellationToken)
    {
        var resources = new List<AiContextResolverResource>();
        var sources = new List<AiContextResolverSource>();

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

            sources.Add(new AiContextResolverSource(entityName, freshContext.Name));

            foreach (var resource in freshContext.Resources.OrderBy(r => r.SortOrder))
            {
                resources.Add(new AiContextResolverResource
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

        return new AiContextResolverResult
        {
            Resources = resources,
            Sources = sources
        };
    }
}
