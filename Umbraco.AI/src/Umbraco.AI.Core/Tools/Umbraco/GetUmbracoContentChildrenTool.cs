using System.ComponentModel;

using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the GetUmbracoContentChildren tool.
/// </summary>
/// <param name="ParentKey">The unique key of the parent content item.</param>
/// <param name="ContentTypeFilter">Optional content type alias to filter children.</param>
/// <param name="Skip">Number of items to skip (for paging).</param>
/// <param name="Take">Number of items to return (for paging).</param>
public record GetUmbracoContentChildrenArgs(
    [property: Description("The unique key (GUID) of the parent content item whose children to list.")]
    Guid ParentKey,

    [property: Description("Optional content type alias to filter children (e.g., 'blogPost', 'article'). Omit to return all children.")]
    string? ContentTypeFilter = null,

    [property: Description("Number of items to skip for paging (default 0).")]
    int? Skip = 0,

    [property: Description("Number of items to return (default 20, max 50).")]
    int? Take = 20);

/// <summary>
/// Tool that lists the children of a published content item.
/// </summary>
[AITool("get_umbraco_content_children", "Get Umbraco Content Children", ScopeId = ContentReadScope.ScopeId)]
public class GetUmbracoContentChildrenTool : AIToolBase<GetUmbracoContentChildrenArgs>
{
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    /// <summary>
    /// Initializes a new instance of <see cref="GetUmbracoContentChildrenTool"/>.
    /// </summary>
    /// <param name="umbracoContextAccessor">The Umbraco context accessor.</param>
    public GetUmbracoContentChildrenTool(IUmbracoContextAccessor umbracoContextAccessor)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
    }

    /// <inheritdoc />
    public override string Description =>
        "Lists the child content items of a given parent content item. " +
        "Use this to navigate the content tree structure (e.g., list blog posts under a blog, pages under a section). " +
        "Supports paging with Skip/Take and optional content type filtering. " +
        "Returns summary info for each child (key, name, content type, URL). " +
        "Use get_umbraco_content with a child's key to get its full property values.";

    /// <inheritdoc />
    protected override Task<object> ExecuteAsync(GetUmbracoContentChildrenArgs args, CancellationToken cancellationToken = default)
    {
        if (args.ParentKey == Guid.Empty)
        {
            return Task.FromResult<object>(new GetUmbracoContentChildrenResult(
                false, [], 0, "Parent key cannot be empty."));
        }

        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
        {
            return Task.FromResult<object>(new GetUmbracoContentChildrenResult(
                false, [], 0, "Umbraco context is not available."));
        }

        var parent = umbracoContext.Content?.GetById(args.ParentKey);
        if (parent is null)
        {
            return Task.FromResult<object>(new GetUmbracoContentChildrenResult(
                false, [], 0, $"Parent content with key '{args.ParentKey}' was not found or is not published."));
        }

        IEnumerable<IPublishedContent> children = parent.Children() ?? [];

        // Apply content type filter
        if (!string.IsNullOrWhiteSpace(args.ContentTypeFilter))
        {
            children = children.Where(c =>
                c.ContentType.Alias.Equals(args.ContentTypeFilter, StringComparison.OrdinalIgnoreCase));
        }

        // Materialize for count before paging
        var allChildren = children.ToList();
        var totalCount = allChildren.Count;

        // Apply paging
        var skip = Math.Max(args.Skip ?? 0, 0);
        var take = Math.Clamp(args.Take ?? 20, 1, 50);

        var pagedChildren = allChildren
            .Skip(skip)
            .Take(take)
            .Select(c => new ContentChildItem(
                c.Key,
                c.Name,
                c.ContentType.Alias,
                c.Url(),
                c.UpdateDate,
                c.SortOrder))
            .ToList();

        return Task.FromResult<object>(new GetUmbracoContentChildrenResult(
            true, pagedChildren, totalCount, null));
    }
}

/// <summary>
/// Result of the get Umbraco content children tool.
/// </summary>
/// <param name="Success">Whether the operation was successful.</param>
/// <param name="Children">The child content items.</param>
/// <param name="TotalCount">The total number of children (before paging).</param>
/// <param name="Message">Optional message (typically for errors).</param>
public record GetUmbracoContentChildrenResult(
    bool Success,
    IReadOnlyList<ContentChildItem> Children,
    int TotalCount,
    string? Message);

/// <summary>
/// Summary info about a child content item.
/// </summary>
/// <param name="Key">The unique key.</param>
/// <param name="Name">The name.</param>
/// <param name="ContentType">The content type alias.</param>
/// <param name="Url">The public URL.</param>
/// <param name="UpdateDate">The last update date.</param>
/// <param name="SortOrder">The sort order within the parent.</param>
public record ContentChildItem(
    Guid Key,
    string Name,
    string ContentType,
    string? Url,
    DateTime UpdateDate,
    int SortOrder);
