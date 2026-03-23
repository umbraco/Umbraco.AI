using System.ComponentModel;

using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the GetContentByRoute tool.
/// </summary>
/// <param name="Route">The URL path to resolve.</param>
/// <param name="Culture">Optional culture code for variant content.</param>
public record GetContentByRouteArgs(
    [property: Description("The URL path to resolve to a content item (e.g., '/about-us', '/blog/my-post'). Do not include the domain, only the path.")]
    string Route,

    [property: Description("Optional culture code for variant content (e.g., 'en-US', 'da-DK'). Omit for the default culture.")]
    string? Culture = null);

/// <summary>
/// Tool that resolves a URL path to a published content item and returns its full property values.
/// Uses <see cref="IDocumentUrlService"/> to resolve routes via the optimised URL cache.
/// </summary>
[AITool("get_content_by_route", "Get Content By Route", ScopeId = ContentReadScope.ScopeId)]
public class GetContentByRouteTool : AIToolBase<GetContentByRouteArgs>
{
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IDocumentUrlService _documentUrlService;

    /// <summary>
    /// Initializes a new instance of <see cref="GetContentByRouteTool"/>.
    /// </summary>
    /// <param name="umbracoContextAccessor">The Umbraco context accessor.</param>
    /// <param name="documentUrlService">The document URL service for route resolution.</param>
    public GetContentByRouteTool(
        IUmbracoContextAccessor umbracoContextAccessor,
        IDocumentUrlService documentUrlService)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
        _documentUrlService = documentUrlService;
    }

    /// <inheritdoc />
    public override string Description =>
        "Resolves a URL path to a published content item and returns its full property values. " +
        "Content editors think in URLs, so use this when the user refers to a page by its URL " +
        "(e.g., 'what's on the /about-us page?'). " +
        "Returns the same detailed content as get_umbraco_content.";

    /// <inheritdoc />
    protected override Task<object> ExecuteAsync(GetContentByRouteArgs args, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(args.Route))
        {
            return Task.FromResult<object>(new GetUmbracoContentResult(
                false, null, "Route cannot be empty."));
        }

        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
        {
            return Task.FromResult<object>(new GetUmbracoContentResult(
                false, null, "Umbraco context is not available."));
        }

        // Ensure route starts with /
        var route = args.Route.StartsWith('/') ? args.Route : "/" + args.Route;

        // Resolve route to document key via the URL cache
        var documentKey = _documentUrlService.GetDocumentKeyByRoute(
            route,
            args.Culture ?? string.Empty,
            null,
            false);

        if (documentKey is null)
        {
            return Task.FromResult<object>(new GetUmbracoContentResult(
                false, null, $"No published content was found at route '{route}'."));
        }

        var content = umbracoContext.Content?.GetById(documentKey.Value);
        if (content is null)
        {
            return Task.FromResult<object>(new GetUmbracoContentResult(
                false, null, $"Content resolved from route '{route}' could not be loaded from the published cache."));
        }

        var properties = PropertyValueFormatter.ExtractProperties(content, args.Culture);

        var parentInfo = content.Parent() is { } parent
            ? new ContentParentItem(parent.Key, parent.Name)
            : null;

        var item = new UmbracoContentItem(
            content.Key,
            content.Name,
            content.ContentType.Alias,
            content.Url(),
            content.CreateDate,
            content.UpdateDate,
            content.Level,
            ContentToolHelpers.GetContentPath(content),
            parentInfo,
            properties);

        return Task.FromResult<object>(new GetUmbracoContentResult(true, item, null));
    }
}
