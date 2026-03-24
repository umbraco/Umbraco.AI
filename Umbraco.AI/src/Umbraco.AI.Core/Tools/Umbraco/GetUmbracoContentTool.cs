using System.ComponentModel;

using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the GetUmbracoContent tool.
/// </summary>
/// <param name="Key">The unique key of the content item.</param>
/// <param name="Culture">Optional culture code for variant content.</param>
public record GetUmbracoContentArgs(
    [property: Description("The unique key (GUID) of the content item to retrieve. Use IDs from search_umbraco results.")]
    Guid Key,

    [property: Description("Optional culture code for variant content (e.g., 'en-US', 'da-DK'). Omit for invariant content.")]
    string? Culture = null);

/// <summary>
/// Tool that retrieves a published content item from Umbraco by its key, including all property values.
/// </summary>
/// <remarks>
/// Uses Umbraco's friendly extension methods (Parent(), Children(), Url(), Ancestors())
/// which internally resolve the navigation query service for optimised tree traversal.
/// </remarks>
[AITool("get_umbraco_content", "Get Umbraco Content", ScopeId = ContentReadScope.ScopeId)]
public class GetUmbracoContentTool : AIToolBase<GetUmbracoContentArgs>
{
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    /// <summary>
    /// Initializes a new instance of <see cref="GetUmbracoContentTool"/>.
    /// </summary>
    /// <param name="umbracoContextAccessor">The Umbraco context accessor.</param>
    public GetUmbracoContentTool(IUmbracoContextAccessor umbracoContextAccessor)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
    }

    /// <inheritdoc />
    public override string Description =>
        "Retrieves a published content item from Umbraco by its unique key (GUID). " +
        "Returns the full content including all property values, content type, URL, parent info, and metadata. " +
        "Use IDs from search_umbraco results to fetch detailed content. " +
        "For variant (multilingual) content, specify the culture code to get culture-specific values.";

    /// <inheritdoc />
    protected override Task<object> ExecuteAsync(GetUmbracoContentArgs args, CancellationToken cancellationToken = default)
    {
        if (args.Key == Guid.Empty)
        {
            return Task.FromResult<object>(new GetUmbracoContentResult(
                false, null, "Content key cannot be empty."));
        }

        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
        {
            return Task.FromResult<object>(new GetUmbracoContentResult(
                false, null, "Umbraco context is not available."));
        }

        var content = umbracoContext.Content?.GetById(args.Key);
        if (content is null)
        {
            return Task.FromResult<object>(new GetUmbracoContentResult(
                false, null, $"Content with key '{args.Key}' was not found or is not published."));
        }

        var item = ContentToolHelpers.BuildContentItem(content, args.Culture);

        return Task.FromResult<object>(new GetUmbracoContentResult(true, item, null));
    }
}

/// <summary>
/// Result of the get Umbraco content tool.
/// </summary>
/// <param name="Success">Whether the content was found.</param>
/// <param name="Content">The content item, if found.</param>
/// <param name="Message">Optional message (typically for errors).</param>
public record GetUmbracoContentResult(
    bool Success,
    UmbracoContentItem? Content,
    string? Message);

/// <summary>
/// A published content item with full property values.
/// </summary>
/// <param name="Key">The unique key of the content item.</param>
/// <param name="Name">The name of the content item.</param>
/// <param name="ContentType">The content type alias.</param>
/// <param name="Url">The public URL of the content item.</param>
/// <param name="CreateDate">The creation date.</param>
/// <param name="UpdateDate">The last update date.</param>
/// <param name="Level">The depth level in the content tree.</param>
/// <param name="Path">The breadcrumb path (e.g., "Home > About > Team").</param>
/// <param name="Parent">The parent content item info, if any.</param>
/// <param name="Properties">The content properties with their values.</param>
public record UmbracoContentItem(
    Guid Key,
    string Name,
    string ContentType,
    string? Url,
    DateTime CreateDate,
    DateTime UpdateDate,
    int Level,
    string Path,
    ContentParentItem? Parent,
    IReadOnlyList<ContentPropertyItem> Properties);

/// <summary>
/// Summary info about a content item's parent.
/// </summary>
/// <param name="Key">The parent's unique key.</param>
/// <param name="Name">The parent's name.</param>
public record ContentParentItem(Guid Key, string Name);
